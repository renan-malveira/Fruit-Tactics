using System;
using System.Collections;
using UnityEngine;

namespace Ads
{
    /// <summary>
    /// Ritmo do intersticial (v2.2):
    /// - nada nas primeiras fases (antes da fase <see cref="FirstLevelWithAds"/>);
    /// - no máximo um a cada <see cref="MatchesBetweenAds"/> partidas e
    ///   <see cref="MinSecondsBetweenAds"/> segundos;
    /// - só na transição (Próxima, Jogar de novo, Menu), nunca por cima da tela de resultado.
    ///   A cena seguinte só carrega quando o anúncio fecha.
    /// </summary>
    public class InterstitialAdManager : MonoBehaviour
    {
        // Cada fase dura ~1 min: 2 partidas + 60 s dá um anúncio a cada 2-3 minutos de jogo.
        private const int MatchesBetweenAds = 2;
        private const int TestModeMatches = 1;
        private const int FirstLevelWithAds = 4;
        private const float MinSecondsBetweenAds = 60f;
        private const float ShowFailSafeSeconds = 60f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private bool testMode = false;
#else
        private const bool testMode = false;
#endif

        [SerializeField] private AdGatingService adGatingService;

        private IInterstitialAdProvider _adProvider;
        private bool _isShowingAd;
        private int _matchesSinceLastAd;
        private float _lastAdRealtime = float.NegativeInfinity;
        private Action _afterAd;
        private Coroutine _failSafe;

        public static InterstitialAdManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _matchesSinceLastAd = 0;

            if (adGatingService != null)
                adGatingService.OnAdStatusChanged += OnAdStatusChanged;

            LoadNextAd();
        }

        private void OnDestroy()
        {
            if (adGatingService != null)
                adGatingService.OnAdStatusChanged -= OnAdStatusChanged;

            if (Instance == this)
                Instance = null;
        }

        private void OnAdStatusChanged()
        {
            if (!ShouldShowAds())
                ResetAdTimer();
        }

        public void SetAdGatingService(IAdGatingService service)
        {
            if (adGatingService != null)
                adGatingService.OnAdStatusChanged -= OnAdStatusChanged;

            adGatingService = service as AdGatingService;

            if (adGatingService != null)
                adGatingService.OnAdStatusChanged += OnAdStatusChanged;
        }

        /// <summary>Fim de uma partida na fase <paramref name="levelNumber"/> (1 = primeira fase). Só conta.</summary>
        public void RegisterMatchCompleted(int levelNumber)
        {
            if (!ShouldShowAds() || levelNumber < FirstLevelWithAds)
                return;

            _matchesSinceLastAd++;
        }

        /// <summary>Compatibilidade (AdGatingTestDriver): conta uma partida sem mostrar nada.</summary>
        public void OnMatchCompleted() => RegisterMatchCompleted(FirstLevelWithAds);

        /// <summary>
        /// Mostra o intersticial se ele estiver na vez e chama <paramref name="continueWith"/>
        /// quando ele fechar. Se não for a vez (ou não houver anúncio pronto), chama na hora.
        /// </summary>
        public void ShowIfDueThen(Action continueWith)
        {
            if (!IsAdDue() || !TryShow("transition", continueWith))
                continueWith?.Invoke();
        }

        private bool IsAdDue()
        {
            if (!ShouldShowAds() || _isShowingAd)
                return false;

            var interval = testMode ? TestModeMatches : MatchesBetweenAds;
            if (_matchesSinceLastAd < interval)
                return false;

            return testMode || Time.realtimeSinceStartup - _lastAdRealtime >= MinSecondsBetweenAds;
        }

        public void SetAdProvider(IInterstitialAdProvider provider)
        {
            _adProvider = provider;
            LoadNextAd();
        }

        /// <summary>Mostra agora, sem olhar o ritmo (painéis de debug).</summary>
        public void ShowInterstitialAd()
        {
            if (!ShouldShowAds() || _isShowingAd)
                return;

            TryShow("debug", null);
        }

        private bool TryShow(string placement, Action afterAd)
        {
            if (_adProvider == null || !_adProvider.IsAdReady())
            {
                LoadNextAd();
                return false;
            }

            _isShowingAd = true;
            _afterAd = afterAd;
            Time.timeScale = 0f;

            Managers.AnalyticsManager.Instance?.TrackAdStarted("interstitial", placement);

            if (_failSafe != null) StopCoroutine(_failSafe);
            _failSafe = StartCoroutine(FailSafe());

            _adProvider.ShowAd(
                onAdCompleted: OnAdCompleted,
                onAdFailed: OnAdFailed
            );
            return true;
        }

        // Se o SDK não avisar que o anúncio fechou, o jogo não fica preso na tela de resultado.
        private IEnumerator FailSafe()
        {
            yield return new WaitForSecondsRealtime(ShowFailSafeSeconds);
            _failSafe = null;
            if (_isShowingAd)
                FinishAd(false);
        }

        private void OnAdCompleted() => FinishAd(true);

        private void OnAdFailed() => FinishAd(false);

        private void FinishAd(bool completed)
        {
            if (!_isShowingAd)
                return;

            if (_failSafe != null)
            {
                StopCoroutine(_failSafe);
                _failSafe = null;
            }

            Managers.AnalyticsManager.Instance?.TrackAdCompleted("interstitial", completed);
            ResetAdTimer();
            if (completed)
                _lastAdRealtime = Time.realtimeSinceStartup;
            ResumeGame();
            LoadNextAd();

            var next = _afterAd;
            _afterAd = null;
            next?.Invoke();
        }

        private bool ShouldShowAds()
        {
            var gating = adGatingService != null ? adGatingService : AdGatingService.Instance;
            return gating == null || gating.ShouldShowAds();
        }

        private void ResetAdTimer()
        {
            _matchesSinceLastAd = 0;
        }

        private void ResumeGame()
        {
            _isShowingAd = false;
            Time.timeScale = 1f;
        }

        private void LoadNextAd()
        {
            _adProvider?.LoadAd();
        }

        public int GetMatchesUntilNextAd()
        {
            int currentInterval = testMode ? TestModeMatches : MatchesBetweenAds;
            return Mathf.Max(0, currentInterval - _matchesSinceLastAd);
        }

        public void ToggleTestMode()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            testMode = !testMode;
            ResetAdTimer();
#endif
        }

        public bool IsTestModeEnabled()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return testMode;
#else
            return false;
#endif
        }

        public void ForceShowAd()
        {
            ResetAdTimer();
            ShowInterstitialAd();
        }
    }
}
