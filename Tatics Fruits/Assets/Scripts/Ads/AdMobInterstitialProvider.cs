using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace Ads
{
    public class AdMobInterstitialProvider : IInterstitialAdProvider
    {
        private readonly string _adUnitId;
        private readonly bool _hasValidUnit;
        private InterstitialAd _interstitialAd;
        private Action _onAdCompleted;
        private Action _onAdFailed;

        public AdMobInterstitialProvider()
        {
            _adUnitId = Core.StoreConfig.InterstitialUnitId;
            _hasValidUnit = Core.StoreConfig.IsUsableAdUnit(_adUnitId);

            if (!_hasValidUnit)
                Debug.LogWarning("[AdMobInterstitialProvider] Nenhuma unidade de intersticial válida para esta plataforma (veja StoreConfig). Anúncios desativados.");
        }

        public bool IsAdReady()
        {
            return _interstitialAd != null && _interstitialAd.CanShowAd();
        }

        public void ShowAd(Action onAdCompleted, Action onAdFailed)
        {
            _onAdCompleted = onAdCompleted;
            _onAdFailed = onAdFailed;

            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                Debug.Log($"[AdMobInterstitialProvider] Showing interstitial ad with ID: {_adUnitId}");
                _interstitialAd.Show();
            }
            else
            {
                Debug.LogError("[AdMobInterstitialProvider] Interstitial ad is not ready to be shown.");
                _onAdFailed?.Invoke();
            }
        }

        public void LoadAd()
        {
            if (!_hasValidUnit)
                return;

            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }

            Debug.Log($"[AdMobInterstitialProvider] Loading interstitial ad with ID: {_adUnitId}");

            AdRequest adRequest = new AdRequest();

            InterstitialAd.Load(_adUnitId, adRequest, OnAdLoadCallback);
        }

        private void OnAdLoadCallback(InterstitialAd ad, LoadAdError error)
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"[AdMobInterstitialProvider] ❌ FAILED TO LOAD AD");
                Debug.LogError($"[AdMobInterstitialProvider] Error Code: {error?.GetCode()}");
                Debug.LogError($"[AdMobInterstitialProvider] Error Domain: {error?.GetDomain()}");
                Debug.LogError($"[AdMobInterstitialProvider] Error Message: {error?.GetMessage()}");
                Debug.LogError($"[AdMobInterstitialProvider] Error Cause: {error?.GetCause()}");
                Debug.LogError($"[AdMobInterstitialProvider] Response Info: {error?.GetResponseInfo()}");
                return;
            }

            Debug.Log($"[AdMobInterstitialProvider] ✅ AD LOADED SUCCESSFULLY!");
            Debug.Log($"[AdMobInterstitialProvider] Response: {ad.GetResponseInfo()}");
            _interstitialAd = ad;
            RegisterEventHandlers(ad);
        }

        private void RegisterEventHandlers(InterstitialAd ad)
        {
            ad.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log($"[AdMobInterstitialProvider] Interstitial ad paid {adValue.Value} {adValue.CurrencyCode}.");
            };

            ad.OnAdImpressionRecorded += () =>
            {
                Debug.Log("[AdMobInterstitialProvider] Interstitial ad recorded an impression.");
            };

            ad.OnAdClicked += () =>
            {
                Debug.Log("[AdMobInterstitialProvider] Interstitial ad was clicked.");
            };

            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("[AdMobInterstitialProvider] Interstitial ad full screen content opened.");
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdMobInterstitialProvider] Interstitial ad full screen content closed.");
                _onAdCompleted?.Invoke();
                LoadAd();
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"[AdMobInterstitialProvider] Interstitial ad failed to open full screen content with error: {error}");
                _onAdFailed?.Invoke();
                LoadAd();
            };
        }

        public void Destroy()
        {
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
                Debug.Log("[AdMobInterstitialProvider] Interstitial ad destroyed.");
            }
        }
    }
}
