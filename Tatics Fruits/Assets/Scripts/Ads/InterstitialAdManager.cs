using System;
using UnityEngine;

namespace Ads
{
    public class InterstitialAdManager : MonoBehaviour
    {
        private const int MatchesBetweenAds = 3;
        private const int TestModeMatches = 1;

        [SerializeField] private bool enableAds = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private bool testMode = false;
#else
        private const bool testMode = false;
#endif
        
        private IInterstitialAdProvider _adProvider;
        private bool _isShowingAd;
        private int _matchesSinceLastAd;

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
            LoadNextAd();
        }

        public void OnMatchCompleted()
        {
            if (!enableAds || _isShowingAd)
                return;

            _matchesSinceLastAd++;

            int currentInterval = testMode ? TestModeMatches : MatchesBetweenAds;

            if (_matchesSinceLastAd >= currentInterval)
                ShowInterstitialAd();
        }

        public void SetAdProvider(IInterstitialAdProvider provider)
        {
            _adProvider = provider;
            LoadNextAd();
        }

        public void ShowInterstitialAd()
        {
            if (!enableAds || _isShowingAd)
                return;

            if (_adProvider == null || !_adProvider.IsAdReady())
            {
                LoadNextAd();
                return;
            }

            _isShowingAd = true;
            Time.timeScale = 0f;
            
            Managers.AnalyticsManager.Instance?.TrackAdStarted("interstitial", "auto");

            _adProvider.ShowAd(
                onAdCompleted: OnAdCompleted,
                onAdFailed: OnAdFailed
            );
        }

        private void OnAdCompleted()
        {
            Managers.AnalyticsManager.Instance?.TrackAdCompleted("interstitial", true);
            ResetAdTimer();
            ResumeGame();
            LoadNextAd();
        }

        private void OnAdFailed()
        {
            Managers.AnalyticsManager.Instance?.TrackAdCompleted("interstitial", false);
            ResetAdTimer();
            ResumeGame();
            LoadNextAd();
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

        public void EnableAds(bool enable)
        {
            enableAds = enable;
        }

        public int GetMatchesUntilNextAd()
        {
            int currentInterval = testMode ? TestModeMatches : MatchesBetweenAds;
            return Mathf.Max(0, currentInterval - _matchesSinceLastAd);
        }

        public void ToggleTestMode()
        {
            testMode = !testMode;
            ResetAdTimer();
        }

        public bool IsTestModeEnabled()
        {
            return testMode;
        }

        public void ForceShowAd()
        {
            ResetAdTimer();
            ShowInterstitialAd();
        }
    }
}
