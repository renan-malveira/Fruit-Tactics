using UnityEngine;
using Core;

namespace Ads
{
    public class AdInitializer : MonoBehaviour
    {
        [Header("Ad Provider Settings")]
        [SerializeField] private AdProviderType providerType = AdProviderType.AdMob;
        
        [Header("Ad Type Settings")]
        [SerializeField] private bool initializeInterstitial = true;
        [SerializeField] private bool initializeBanner = true;

        private void Awake()
        {
            if (providerType == AdProviderType.AdMob)
            {
                SetupGoogleMobileAdsInitializer();
            }
        }

        private void Start()
        {
            if (providerType == AdProviderType.AdMob)
            {
                StartCoroutine(InitializeAdsAfterDelay());
            }
            else
            {
                InitializeAds();
            }
        }

        private void SetupGoogleMobileAdsInitializer()
        {
            if (GetComponent<GoogleMobileAdsInitializer>() == null)
            {
                gameObject.AddComponent<GoogleMobileAdsInitializer>();
            }
        }

        private System.Collections.IEnumerator InitializeAdsAfterDelay()
        {
            // Espera o fluxo de consentimento (LGPD -> UMP -> ATT) terminar.
            while (!Core.Privacy.ConsentManager.IsResolved)
                yield return new WaitForSeconds(0.5f);

            if (!Core.Privacy.ConsentManager.CanRequestAds)
            {
                Debug.LogWarning("[AdInitializer] Consentimento não permite anúncios. Provedores não serão criados.");
                yield break;
            }

            while (!GoogleMobileAdsInitializer.IsInitialized())
                yield return new WaitForSeconds(0.5f);

            Debug.Log("[AdInitializer] SDK initialized! Now initializing ad providers...");
            InitializeAds();
        }

        private void InitializeAds()
        {
            if (initializeInterstitial)
            {
                InitializeInterstitialAds();
            }

            if (initializeBanner)
            {
                InitializeBannerAds();
            }
        }

        private void InitializeInterstitialAds()
        {
            IInterstitialAdProvider interstitialProvider = providerType switch
            {
                AdProviderType.TestProvider => CreateTestInterstitialProvider(),
                AdProviderType.UnityAds => new UnityAdsInterstitialProvider(),
                AdProviderType.AdMob => new AdMobInterstitialProvider(),
                _ => null
            };

            if (interstitialProvider != null && InterstitialAdManager.Instance != null)
            {
                InterstitialAdManager.Instance.SetAdProvider(interstitialProvider);
                Debug.Log($"[AdInitializer] Interstitial ad provider initialized: {providerType}");
            }
            else
            {
                Debug.LogError($"[AdInitializer] Failed to initialize interstitial ad provider or InterstitialAdManager not found. Provider: {interstitialProvider != null}, Manager: {InterstitialAdManager.Instance != null}");
            }
        }

        private void InitializeBannerAds()
        {
            IBannerAdProvider bannerProvider = providerType switch
            {
                AdProviderType.TestProvider => CreateTestBannerProvider(),
                AdProviderType.AdMob => new AdMobBannerProvider(),
                _ => null
            };

            if (bannerProvider != null && BannerAdManager.Instance != null)
            {
                BannerAdManager.Instance.SetBannerProvider(bannerProvider);
                Debug.Log($"[AdInitializer] Banner ad provider initialized: {providerType}");
            }
            else
            {
                Debug.LogError($"[AdInitializer] Failed to initialize banner ad provider or BannerAdManager not found. Provider: {bannerProvider != null}, Manager: {BannerAdManager.Instance != null}");
            }
        }

        private IInterstitialAdProvider CreateTestInterstitialProvider()
        {
            TestAdProvider testProvider = gameObject.AddComponent<TestAdProvider>();
            return testProvider;
        }

        private IBannerAdProvider CreateTestBannerProvider()
        {
            TestBannerProvider testBannerProvider = gameObject.AddComponent<TestBannerProvider>();
            return testBannerProvider;
        }

        private enum AdProviderType
        {
            TestProvider,
            UnityAds,
            AdMob
        }
    }
}
