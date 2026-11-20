using GoogleMobileAds.Api;
using UnityEngine;

namespace Ads
{
    public class AdMobBannerProvider : IBannerAdProvider
    {
        private readonly string _adUnitId;
        private readonly bool _hasValidUnit;
        private BannerView _bannerView;
        private bool _isBannerShowing;

        public AdMobBannerProvider()
        {
            _adUnitId = Core.StoreConfig.BannerUnitId;
            _hasValidUnit = Core.StoreConfig.IsUsableAdUnit(_adUnitId);

            if (!_hasValidUnit)
                Debug.LogWarning("[AdMobBannerProvider] Nenhuma unidade de banner válida para esta plataforma (veja StoreConfig). Banner desativado.");
        }

        public void LoadAndShowBanner()
        {
            if (!_hasValidUnit)
                return;

            if (_bannerView != null)
            {
                Debug.Log("[AdMobBannerProvider] Banner already exists. Showing existing banner.");
                _bannerView.Show();
                _isBannerShowing = true;
                return;
            }

            Debug.Log($"[AdMobBannerProvider] Creating and loading banner ad with ID: {_adUnitId}");

            // Banner adaptativo (v2.2): ocupa a largura da tela, com a altura que o Google
            // escolhe para o aparelho. Rende mais que o 320x50 fixo e usa a mesma unidade.
            var adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            _bannerView = new BannerView(_adUnitId, adaptiveSize, AdPosition.Bottom);

            RegisterEventHandlers();

            AdRequest adRequest = new AdRequest();
            _bannerView.LoadAd(adRequest);
        }

        public void HideBanner()
        {
            if (_bannerView != null)
            {
                Debug.Log("[AdMobBannerProvider] Hiding banner ad");
                _bannerView.Hide();
                _isBannerShowing = false;
            }
        }

        public void DestroyBanner()
        {
            if (_bannerView != null)
            {
                Debug.Log("[AdMobBannerProvider] Destroying banner ad");
                _bannerView.Destroy();
                _bannerView = null;
                _isBannerShowing = false;
            }
        }

        public bool IsBannerShowing()
        {
            return _isBannerShowing;
        }

        private void RegisterEventHandlers()
        {
            _bannerView.OnBannerAdLoaded += () =>
            {
                Debug.Log("[AdMobBannerProvider] ✅ Banner ad loaded successfully!");
                _isBannerShowing = true;
            };

            _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                Debug.LogError($"[AdMobBannerProvider] ❌ Banner ad failed to load");
                Debug.LogError($"[AdMobBannerProvider] Error Code: {error.GetCode()}");
                Debug.LogError($"[AdMobBannerProvider] Error Message: {error.GetMessage()}");
                _isBannerShowing = false;
            };

            _bannerView.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log($"[AdMobBannerProvider] Banner ad paid {adValue.Value} {adValue.CurrencyCode}");
            };

            _bannerView.OnAdImpressionRecorded += () =>
            {
                Debug.Log("[AdMobBannerProvider] Banner ad impression recorded");
            };

            _bannerView.OnAdClicked += () =>
            {
                Debug.Log("[AdMobBannerProvider] Banner ad clicked");
            };

            _bannerView.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("[AdMobBannerProvider] Banner ad full screen content opened");
            };

            _bannerView.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdMobBannerProvider] Banner ad full screen content closed");
            };
        }
    }
}
