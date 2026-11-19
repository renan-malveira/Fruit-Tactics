using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Gameplay.Controllers;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

using UnityEngine;


namespace Managers
{
    public class LoginManager : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private DataSaver dataSaver;

        private FirebaseAuth _firebaseAuth;
        private FirebaseUser _currentUser;

        public static LoginManager Instance { get; private set; }

        private async void Awake()
        {
            // A MainMenu traz um LoginManager novo a cada visita. Antes os dois ficavam vivos e o
            // antigo seguia usando o DataSaver da visita anterior (já destruído). Agora o primeiro
            // continua e só recebe o DataSaver novo da cena.
            if (Instance != null && Instance != this)
            {
                Instance.AdoptSceneDataSaver(dataSaver);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            await InitializeFirebaseAsync();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Recebe o DataSaver da cena recém-carregada e, se já houver login, carrega os dados nele.
        /// </summary>
        private void AdoptSceneDataSaver(DataSaver sceneDataSaver)
        {
            if (sceneDataSaver == null || sceneDataSaver == dataSaver)
                return;

            dataSaver = sceneDataSaver;

            if (_currentUser != null)
                OnFirebaseSignedIn(_currentUser);
        }


        private async Task InitializeFirebaseAsync()
        {
            try
            {
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            
                if (dependencyStatus == DependencyStatus.Available)
                {
                    // Firebase Analytics começa desligado (manifest/Info.plist) e só liga com consentimento.
                    Core.Privacy.ConsentManager.WhenResolved(() =>
                        Firebase.Analytics.FirebaseAnalytics.SetAnalyticsCollectionEnabled(
                            Core.Privacy.ConsentManager.CanCollectAnalytics));

                    _firebaseAuth = FirebaseAuth.DefaultInstance;
                    _currentUser = _firebaseAuth.CurrentUser;

                    if (_currentUser != null)
                        OnFirebaseSignedIn(_currentUser);
                    else
                        SignInAsGuest();
                }
                else
                {
                    Debug.LogError($"[LoginManager] Could not resolve Firebase dependencies: {dependencyStatus}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoginManager] Error initializing Firebase: {e}");
            }
        }

#if UNITY_ANDROID
        public void LoginGooglePlayGames()
        {
            PlayGamesPlatform.Instance.Authenticate((status) =>
            {
                if (status == SignInStatus.Success)
                {
                    PlayGamesPlatform.Instance.RequestServerSideAccess(false, idToken =>
                    {
                        SignInWithGooglePlayGamesFirebase(idToken);
                    });
                }
                else
                {
                    Debug.LogError($"[LoginManager] Google Play Games authentication failed: {status}");
                }
            });
        }
#else
        public void LoginGooglePlayGames()
        {
            SignInAsGuest();
        }
#endif


#if UNITY_ANDROID
        private void SignInWithGooglePlayGamesFirebase(string idToken)
        {
            if (string.IsNullOrEmpty(idToken))
            {
                Debug.LogError("[LoginManager] ID Token is null or empty!");
                return;
            }

            Credential credential = PlayGamesAuthProvider.GetCredential(idToken);
    
            _firebaseAuth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"[LoginManager] Firebase sign-in failed: {task.Exception}");
                    return;
                }

                _currentUser = task.Result;
                OnFirebaseSignedIn(_currentUser);
            });
        }
#else
        private void SignInWithGooglePlayGamesFirebase(string idToken) { }
#endif


        public void SignInAsGuest()
        {
            _firebaseAuth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"[LoginManager] Guest sign-in failed: {task.Exception}");
                    return;
                }

                _currentUser = task.Result.User;
                OnFirebaseSignedIn(_currentUser);
            });
        }

        private void OnFirebaseSignedIn(FirebaseUser user)
        {
            if (user == null)
            {
                Debug.LogError("[LoginManager] User is null in OnFirebaseSignedIn!");
                return;
            }

            var uid = user.UserId;
            var displayName = user.DisplayName ?? "Guest";

            if (dataSaver == null)
            {
                Debug.LogError("[LoginManager] DataSaver is null!");
                return;
            }

            dataSaver.SetUserId(uid);
            
            dataSaver.OnDataLoaded -= HandleDataLoaded;
            dataSaver.OnLoadFailed -= HandleLoadFailed;
            dataSaver.OnDataNotFound -= HandleDataNotFound;

            dataSaver.OnDataLoaded += HandleDataLoaded;
            dataSaver.OnLoadFailed += HandleLoadFailed;
            dataSaver.OnDataNotFound += HandleDataNotFound;

            dataSaver.LoadData();

            void HandleDataLoaded(DataToSave cloud)
            {
                ApplyCloudDataToProfile(cloud);
            }

            void HandleDataNotFound()
            {
                dataSaver.OnDataLoaded -= HandleDataLoaded;
                dataSaver.OnLoadFailed -= HandleLoadFailed;
                dataSaver.OnDataNotFound -= HandleDataNotFound;
            }

            void HandleLoadFailed(Exception e)
            {
                Debug.LogError($"[LoginManager] Load failed: {e}");
                dataSaver.OnDataLoaded -= HandleDataLoaded;
                dataSaver.OnLoadFailed -= HandleLoadFailed;
                dataSaver.OnDataNotFound -= HandleDataNotFound;
            }

            void ApplyCloudDataToProfile(DataToSave cloud)
            {
                var profileController = FindFirstObjectByType<PlayerProfileController>();
                if (profileController == null || profileController.Data == null)
                    return;
                
                bool cloudIsNewer = cloud.lastUpdatedTicks > profileController.Data.lastUpdatedTicks;

                profileController.Data.playerName = string.IsNullOrEmpty(displayName) || displayName == "Guest"
                    ? cloud.userName
                    : displayName;

                // Fases liberadas, recordes e estrelas só sobem: junta nuvem e aparelho pelo maior valor,
                // venha de onde vier o dado mais novo (troca de aparelho não perde progresso).
                var data = profileController.Data;
                data.highestLevelUnlocked = Mathf.Max(data.highestLevelUnlocked, cloud.highScore);
                data.BestScores = Core.SaveSystem.IntMapSerializer.MergeMax(data.BestScores, cloud.BestScores);
                data.LevelStars = Core.SaveSystem.IntMapSerializer.MergeMax(data.LevelStars, cloud.LevelStars);

                if (cloudIsNewer)
                {
                    profileController.Data.gold = cloud.totalCoins;
                    profileController.Data.currentLevelIndex = Mathf.Max(0, cloud.crrLevel);

                    if (cloud.ownedCards != null) profileController.Data.ownedCards = new List<string>(cloud.ownedCards);
                    if (cloud.equippedDeck != null) profileController.Data.equippedDeck = new List<string>(cloud.equippedDeck);
                    if (cloud.unlockedAvatar != null) profileController.Data.unlockedAvatars = new List<int>(cloud.unlockedAvatar);
                    if (cloud.purchasedAvatar != null) profileController.Data.purchasedAvatars = new List<int>(cloud.purchasedAvatar);

                    profileController.Data.musicOn = cloud.musicOn;
                    profileController.Data.sfxOn = cloud.sfxOn;
                    profileController.Data.vfxOn = cloud.vfxOn;
                    profileController.Data.language = cloud.language;

                    if (profileController.Data.daily != null)
                    {
                        profileController.Data.daily.dayKey = cloud.dailyDayKey ?? "";
                        if (profileController.Data.daily.login != null)
                            profileController.Data.daily.login.lastClaimDayKey = cloud.lastLoginDayKey ?? "";
                    }
                }

                profileController.Data.firebaseUserId = uid;

                if (dataSaver?.dataToSave != null)
                {
                    dataSaver.dataToSave.totalCoins = profileController.Data.gold;
                    dataSaver.dataToSave.lastUpdatedTicks = profileController.Data.lastUpdatedTicks;
                    dataSaver.SaveLocal();
                }

                profileController.SaveProfile();
                profileController.SetFirebaseUserId(uid);

                dataSaver.OnDataLoaded -= HandleDataLoaded;
                dataSaver.OnLoadFailed -= HandleLoadFailed;
                dataSaver.OnDataNotFound -= HandleDataNotFound;
            }
        }

        public bool IsSignedIn()
        {
            return _currentUser != null;
        }

        public string GetUserId()
        {
            return _currentUser?.UserId;
        }
    }
}
