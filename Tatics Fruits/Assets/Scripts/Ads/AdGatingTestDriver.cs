using System;
using Managers;
using UnityEngine;

namespace Ads
{
    public class AdGatingTestDriver : MonoBehaviour
    {
        [SerializeField] private DataSaver dataSaver;
        [SerializeField] private InterstitialAdManager interstitialAdManager;

        [Header("Test Controls")]
        [SerializeField] private bool simulateRemoveAds;
        [SerializeField] private bool simulateVip;
        [SerializeField] private bool simulateExpiredVip;
        [SerializeField] private bool simulateReset;
        [SerializeField] private bool simulateMatchCompleted;

        private MockAdProvider _mockProvider;

        private void Start()
        {
            _mockProvider = new MockAdProvider();
            interstitialAdManager.SetAdProvider(_mockProvider);
        }

        private void Update()
        {
            if (simulateReset)          { simulateReset = false;          ResetState(); }
            if (simulateRemoveAds)      { simulateRemoveAds = false;      ApplyRemoveAds(); }
            if (simulateVip)            { simulateVip = false;            ApplyVip(); }
            if (simulateExpiredVip)     { simulateExpiredVip = false;     ApplyExpiredVip(); }
            if (simulateMatchCompleted) { simulateMatchCompleted = false; interstitialAdManager.OnMatchCompleted(); }
        }

        private void ResetState()
        {
            dataSaver.dataToSave.removeAds = false;
            dataSaver.dataToSave.isVip = false;
            dataSaver.dataToSave.vipExpirationTicks = 0;
            AdGatingService.Instance.NotifyStatusChanged();
            _mockProvider.ResetCount();
            Debug.Log($"[TestDriver] State reset | ShouldShowAds={AdGatingService.Instance.ShouldShowAds()} (expected: True)");
        }

        private void ApplyRemoveAds()
        {
            ResetState();
            dataSaver.SetRemoveAds(true);
            Debug.Log($"[TestDriver] removeAds=true | ShouldShowAds={AdGatingService.Instance.ShouldShowAds()} (expected: False)");
        }

        private void ApplyVip()
        {
            ResetState();
            dataSaver.dataToSave.isVip = true;
            dataSaver.dataToSave.vipExpirationTicks = DateTime.UtcNow.AddDays(30).Ticks;
            AdGatingService.Instance.NotifyStatusChanged();
            Debug.Log($"[TestDriver] VIP active 30d | ShouldShowAds={AdGatingService.Instance.ShouldShowAds()} (expected: False)");
        }

        private void ApplyExpiredVip()
        {
            ResetState();
            dataSaver.dataToSave.isVip = true;
            dataSaver.dataToSave.vipExpirationTicks = DateTime.UtcNow.AddDays(-1).Ticks;
            AdGatingService.Instance.NotifyStatusChanged();
            Debug.Log($"[TestDriver] VIP expired | ShouldShowAds={AdGatingService.Instance.ShouldShowAds()} (expected: True)");
        }
    }

    public class MockAdProvider : IInterstitialAdProvider
    {
        private bool _isReady = true;
        private int _showCount;

        public bool IsAdReady() => _isReady;
        public void LoadAd() => _isReady = true;

        public void ShowAd(Action onAdCompleted, Action onAdFailed)
        {
            _showCount++;
            Debug.Log($"[MockAdProvider] Ad shown! Total shown: {_showCount}");
            onAdCompleted?.Invoke();
        }

        public void ResetCount() => _showCount = 0;
        public int ShowCount => _showCount;
    }
}
