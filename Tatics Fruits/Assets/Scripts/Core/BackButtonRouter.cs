using System;
using System.Collections.Generic;
using New_GameplayCore.Views;
using UI.Views;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    /// <summary>
    /// Botão Voltar do Android (v2.2). Antes ele fechava o jogo em qualquer tela.
    /// Ordem: o popup registrado mais recente; depois a regra da cena:
    /// - Gameplay: fecha o popup de sair, fecha o menu de pausa ou abre a pausa;
    /// - MainMenu: fecha as Configurações; sem nada aberto, manda o app para segundo plano;
    /// - demais cenas: manda o app para segundo plano.
    /// Para um popup novo: BackButtonRouter.Push(fechar) ao abrir e Remove(fechar) ao fechar.
    /// </summary>
    public static class BackButtonRouter
    {
        private const string GameplayScene = "Gameplay Scene";
        private const string MainMenuScene = "MainMenu";

        private static readonly List<Func<bool>> Handlers = new();

        public static void Push(Func<bool> handler)
        {
            if (handler != null && !Handlers.Contains(handler))
                Handlers.Add(handler);
        }

        public static void Remove(Func<bool> handler)
        {
            if (handler != null)
                Handlers.Remove(handler);
        }

        public static void HandleBack()
        {
            for (var i = Handlers.Count - 1; i >= 0; i--)
            {
                var handler = Handlers[i];
                try
                {
                    if (handler != null && handler())
                        return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[BackButtonRouter] Handler removido após erro: {e.Message}");
                    Handlers.RemoveAt(i);
                }
            }

            var scene = SceneManager.GetActiveScene().name;
            if (scene == GameplayScene)
            {
                HandleGameplayBack();
                return;
            }

            if (scene == MainMenuScene)
            {
                var settings = UnityEngine.Object.FindFirstObjectByType<SettingsMenu>();
                if (settings != null && settings.IsOpen)
                {
                    settings.Hide();
                    return;
                }
            }

            MoveAppToBackground();
        }

        private static void HandleGameplayBack()
        {
            // Telas de resultado têm os próprios botões (e o anúncio da transição).
            if (UnityEngine.Object.FindFirstObjectByType<VictoryView>() != null ||
                UnityEngine.Object.FindFirstObjectByType<DefeatView>() != null ||
                UnityEngine.Object.FindFirstObjectByType<AllLevelsCompletedView>() != null)
                return;

            var pause = UnityEngine.Object.FindFirstObjectByType<GameplaySettings>();
            if (pause != null)
                pause.HandleBack();
        }

        public static void MoveAppToBackground()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call<bool>("moveTaskToBack", true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BackButtonRouter] moveTaskToBack falhou: {e.Message}");
            }
#endif
        }
    }
}
