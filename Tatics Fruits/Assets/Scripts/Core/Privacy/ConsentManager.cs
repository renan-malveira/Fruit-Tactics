using System;
using System.Collections.Generic;
using Core.SaveSystem;
using Gameplay.Utils;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Core.Privacy
{
    /// <summary>
    /// Ordem de privacidade do app:
    ///   1. Termos/LGPD aceitos (LgpdView)
    ///   2. Formulário de consentimento do Google (UMP) — aparece só onde a lei exige (EEE/Reino Unido/etc.)
    ///   3. Pedido de rastreamento do iOS (ATT), se ainda não decidido
    ///   4. Libera anúncios, analytics e push
    ///
    /// Quem depende disso usa <see cref="WhenResolved"/> em vez de inicializar no Start.
    /// O objeto é criado sozinho na primeira cena; não precisa estar em nenhuma cena.
    /// </summary>
    public class ConsentManager : MonoBehaviour
    {
        private static ConsentManager _instance;
        private static readonly List<Action> PendingCallbacks = new();
        private static readonly Queue<Action> MainThreadQueue = new();

        private bool _flowStarted;

        /// <summary>Fluxo terminou (com ou sem erro). Antes disso nada de anúncio, analytics ou push.</summary>
        public static bool IsResolved { get; private set; }

        /// <summary>O SDK do AdMob pode ser inicializado e anúncios podem ser pedidos.</summary>
        public static bool CanRequestAds { get; private set; }

        /// <summary>
        /// Unity Analytics / Firebase Analytics podem coletar.
        /// Conservador: em regiões onde o UMP exige consentimento, fica desligado,
        /// porque o formulário do Google não cobre esses SDKs.
        /// </summary>
        public static bool CanCollectAnalytics { get; private set; }

        /// <summary>Usuário aceitou os termos; push pode ser pedido.</summary>
        public static bool CanAskForNotifications { get; private set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Só em build de desenvolvimento: simula usuário no EEE para testar o formulário.</summary>
        public static bool DebugForceEea = false;

        /// <summary>Só em build de desenvolvimento: IDs de teste (aparecem no logcat/Xcode na primeira execução).</summary>
        public static readonly List<string> DebugTestDeviceHashedIds = new();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[ConsentManager]");
            _instance = go.AddComponent<ConsentManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (HasAcceptedTerms())
                BeginFlow();
        }

        private void Update()
        {
            lock (MainThreadQueue)
            {
                while (MainThreadQueue.Count > 0)
                    MainThreadQueue.Dequeue()?.Invoke();
            }
        }

        // -----------------------------------------------------------------
        // API pública
        // -----------------------------------------------------------------

        /// <summary>Executa agora se já resolvido; senão, quando o fluxo terminar. Sempre na thread principal.</summary>
        public static void WhenResolved(Action action)
        {
            if (action == null) return;

            if (IsResolved)
            {
                action();
                return;
            }

            PendingCallbacks.Add(action);
        }

        /// <summary>Chamado pela tela de termos (LGPD) quando o jogador aceita.</summary>
        public static void NotifyTermsAccepted()
        {
            if (_instance == null) Bootstrap();
            _instance.BeginFlow();
        }

        /// <summary>
        /// O UMP exige um ponto de acesso para o jogador rever a escolha quando
        /// ele está numa região regulada. Use para mostrar/esconder o botão nas Configurações.
        /// </summary>
        public static bool IsPrivacyOptionsRequired
        {
            get
            {
#if UNITY_EDITOR
                return false;
#else
                return ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
#endif
            }
        }

        public static void ShowPrivacyOptions(Action onClosed = null)
        {
#if UNITY_EDITOR
            onClosed?.Invoke();
#else
            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                RunOnMainThread(() =>
                {
                    if (error != null)
                        Debug.LogWarning($"[ConsentManager] Falha ao abrir opções de privacidade: {error.Message}");

                    RefreshFlags();
                    onClosed?.Invoke();
                });
            });
#endif
        }

        /// <summary>Usado na exclusão de conta: esquece consentimentos para o próximo início.</summary>
        public static void ResetAll()
        {
#if !UNITY_EDITOR
            ConsentInformation.Reset();
#endif
            IsResolved = false;
            CanRequestAds = false;
            CanCollectAnalytics = false;
            CanAskForNotifications = false;
            PendingCallbacks.Clear();
            if (_instance != null) _instance._flowStarted = false;
        }

        // -----------------------------------------------------------------
        // Fluxo
        // -----------------------------------------------------------------

        private static bool HasAcceptedTerms()
        {
            var profile = SaveManager.Instance.Load<PlayerProfileData>();
            return profile != null && profile.hasAcceptedLGPD;
        }

        private void BeginFlow()
        {
            if (_flowStarted) return;
            _flowStarted = true;

#if UNITY_EDITOR
            // O UMP não roda de verdade no Editor.
            Finish(editorFallback: true);
#else
            var request = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false,
#if DEVELOPMENT_BUILD
                ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = DebugForceEea ? DebugGeography.EEA : DebugGeography.Disabled,
                    TestDeviceHashedIds = DebugTestDeviceHashedIds
                }
#endif
            };

            ConsentInformation.Update(request, updateError =>
            {
                RunOnMainThread(() =>
                {
                    if (updateError != null)
                    {
                        // Sem rede, por exemplo. Segue com o que já estava salvo do último uso.
                        Debug.LogWarning($"[ConsentManager] Falha ao atualizar consentimento: {updateError.Message}");
                        RequestTrackingThenFinish();
                        return;
                    }

                    ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                    {
                        RunOnMainThread(() =>
                        {
                            if (formError != null)
                                Debug.LogWarning($"[ConsentManager] Falha no formulário de consentimento: {formError.Message}");

                            RequestTrackingThenFinish();
                        });
                    });
                });
            });
#endif
        }

        private void RequestTrackingThenFinish()
        {
            // Se a mensagem de IDFA estiver configurada no AdMob, o UMP já mostrou o ATT
            // e aqui ele volta direto. Caso contrário, pedimos nós mesmos.
            TrackingAuthorization.RequestIfNeeded(_ => RunOnMainThread(() => Finish(editorFallback: false)));
        }

        private static void Finish(bool editorFallback)
        {
            if (editorFallback)
            {
                CanRequestAds = true;
                CanCollectAnalytics = true;
                CanAskForNotifications = true;
            }
            else
            {
                RefreshFlags();
            }

            IsResolved = true;
            Debug.Log($"[ConsentManager] Resolvido. Ads={CanRequestAds} Analytics={CanCollectAnalytics} Push={CanAskForNotifications}");

            var callbacks = PendingCallbacks.ToArray();
            PendingCallbacks.Clear();
            foreach (var cb in callbacks)
            {
                try { cb(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private static void RefreshFlags()
        {
#if UNITY_EDITOR
            CanRequestAds = true;
            CanCollectAnalytics = true;
#else
            CanRequestAds = ConsentInformation.CanRequestAds();

            var status = ConsentInformation.ConsentStatus;
            var regulatedRegion = status == ConsentStatus.Required || status == ConsentStatus.Obtained;
            CanCollectAnalytics = !regulatedRegion;
#endif
            CanAskForNotifications = true;
        }

        private static void RunOnMainThread(Action action)
        {
            lock (MainThreadQueue)
                MainThreadQueue.Enqueue(action);
        }
    }
}
