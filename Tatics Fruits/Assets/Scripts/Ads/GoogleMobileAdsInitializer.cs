using GoogleMobileAds.Api;
using UnityEngine;
using Core;

namespace Ads
{
    public class GoogleMobileAdsInitializer : MonoBehaviour
    {
        private static bool _isInitialized = false;

        private void Start()
        {
            // O SDK só pode ser inicializado depois do consentimento (UMP/ATT).
            Core.Privacy.ConsentManager.WhenResolved(() =>
            {
                if (this == null) return;

                if (!Core.Privacy.ConsentManager.CanRequestAds)
                {
                    Debug.LogWarning("[GoogleMobileAdsInitializer] Consentimento não permite anúncios. SDK não inicializado.");
                    return;
                }

                InitializeGoogleMobileAds();
            });
        }

        private void InitializeGoogleMobileAds()
        {
            if (_isInitialized)
            {
                Debug.Log("[GoogleMobileAdsInitializer] Google Mobile Ads SDK already initialized.");
                return;
            }

            Debug.Log("[GoogleMobileAdsInitializer] 🚀 INITIALIZING GOOGLE MOBILE ADS SDK...");
            Debug.Log($"[GoogleMobileAdsInitializer] Platform: {Application.platform}");
            Debug.Log($"[GoogleMobileAdsInitializer] Internet Reachability: {Application.internetReachability}");

            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            MobileAds.Initialize((InitializationStatus initStatus) =>
            {
                if (initStatus == null)
                {
                    Debug.LogError("[GoogleMobileAdsInitializer] ❌ INITIALIZATION FAILED - Status is NULL!");
                    return;
                }

                _isInitialized = true;
                Debug.Log("[GoogleMobileAdsInitializer] ✅ INITIALIZATION COMPLETE!");

                var adapterStatusMap = initStatus.getAdapterStatusMap();
                foreach (var item in adapterStatusMap)
                {
                    string statusIcon = item.Value.InitializationState == GoogleMobileAds.Api.AdapterState.Ready ? "✅" : "⚠️";
                    Debug.Log($"[GoogleMobileAdsInitializer] {statusIcon} Adapter: {item.Key}");
                    Debug.Log($"[GoogleMobileAdsInitializer]    Status: {item.Value.InitializationState}");
                    Debug.Log($"[GoogleMobileAdsInitializer]    Latency: {item.Value.Latency}ms");
                    Debug.Log($"[GoogleMobileAdsInitializer]    Description: {item.Value.Description}");
                }
            });
        }

        public static bool IsInitialized()
        {
            return _isInitialized;
        }
    }
}
