using System;
using System.IO;
using System.Threading.Tasks;
using Core.SaveSystem;
using Firebase.Auth;
using Firebase.Database;
using Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Privacy
{
    /// <summary>
    /// Exclusão de conta completa (Apple 5.1.1(v) e política de exclusão de dados do Google Play):
    ///   1. dados na nuvem: users/{uid} e leaderboard/{uid}
    ///   2. usuário do Firebase Auth
    ///   3. pedido de exclusão dos dados do Unity Analytics
    ///   4. todos os arquivos locais, backups e PlayerPrefs
    ///   5. consentimentos (o jogador volta a ver os termos)
    /// Só apaga o que é local depois que a nuvem confirmou, para não deixar dados órfãos.
    /// </summary>
    public static class AccountDeletionService
    {
        public enum Result
        {
            /// <summary>Tudo apagado, inclusive o login.</summary>
            Deleted,
            /// <summary>Dados apagados; o login no Firebase não pôde ser removido (exige login recente) e foi encerrado.</summary>
            DataDeletedLoginSignedOut,
            /// <summary>Sem internet. Nada foi apagado.</summary>
            Offline,
            /// <summary>A nuvem não respondeu ou recusou. Nada local foi apagado.</summary>
            Failed
        }

        private const int RemoteTimeoutMs = 15000;

        public static async Task<Result> DeleteAccountAsync()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return Result.Offline;

            FirebaseAuth auth = null;
            FirebaseUser user = null;
            try
            {
                auth = FirebaseAuth.DefaultInstance;
                user = auth?.CurrentUser;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AccountDeletion] Firebase indisponível: {e.Message}");
            }

            // Para qualquer escrita automática enquanto apagamos.
            FreezeWriters();

            var loginRemoved = true;

            if (user != null)
            {
                var uid = user.UserId;
                try
                {
                    var root = FirebaseDatabase.DefaultInstance.RootReference;
                    var removeProfile = root.Child("users").Child(uid).RemoveValueAsync();
                    var removeRanking = root.Child("leaderboard").Child(uid).RemoveValueAsync();
                    var both = Task.WhenAll(removeProfile, removeRanking);

                    if (await Task.WhenAny(both, Task.Delay(RemoteTimeoutMs)) != both || both.IsFaulted)
                    {
                        Debug.LogError($"[AccountDeletion] Falha ao apagar dados na nuvem: {both.Exception?.GetBaseException().Message ?? "timeout"}");
                        return Result.Failed;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AccountDeletion] Erro ao apagar dados na nuvem: {e.Message}");
                    return Result.Failed;
                }

                try
                {
                    var deleteUser = user.DeleteAsync();
                    if (await Task.WhenAny(deleteUser, Task.Delay(RemoteTimeoutMs)) != deleteUser || deleteUser.IsFaulted)
                    {
                        // Contas vinculadas ao Google Play Games podem exigir login recente.
                        Debug.LogWarning($"[AccountDeletion] Login não removido: {deleteUser.Exception?.GetBaseException().Message ?? "timeout"}");
                        loginRemoved = false;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AccountDeletion] Login não removido: {e.Message}");
                    loginRemoved = false;
                }

                try { auth.SignOut(); }
                catch (Exception e) { Debug.LogWarning($"[AccountDeletion] SignOut: {e.Message}"); }
            }

            AnalyticsManager.Instance?.StopAndRequestDataDeletion();

            DeleteLocalData();
            ConsentManager.ResetAll();

            return loginRemoved ? Result.Deleted : Result.DataDeletedLoginSignedOut;
        }

        private static void FreezeWriters()
        {
            PlayerDataManager.Instance?.EnableAutoSave(false);

            foreach (var saver in Object.FindObjectsByType<DataSaver>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                saver.ResetForAccountDeletion();
        }

        private static void DeleteLocalData()
        {
            // Arquivos .json do SaveManager, JsonDataService e DataSaver + cache em memória.
            SaveManager.Instance.DeleteAll();

            var root = Application.persistentDataPath;
            TryDeleteDirectory(Path.Combine(root, "Backups"));

            SettingsRepository.ClearCache();

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AccountDeletion] Não foi possível apagar {path}: {e.Message}");
            }
        }
    }
}
