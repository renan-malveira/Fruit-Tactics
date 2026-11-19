using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ads
{
    public class BannerAdManager : MonoBehaviour
    {
        [Header("Banner Settings")]
        [SerializeField] private bool showBannerOnStart = true;
        [SerializeField] private bool enableBanners = true;
        
        [Header("Scene Control")]
        [Tooltip("List of scene names where banners should be shown")]
        [SerializeField] private string[] scenesWithBanners = { "MainMenu", "Loading"};

        private IBannerAdProvider _bannerProvider;
        private bool _isCurrentSceneAllowed;
        private AdGatingService _gating;

        public static BannerAdManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            HookGating();
            CheckCurrentScene();
            
            if (enableBanners && showBannerOnStart && _bannerProvider != null && _isCurrentSceneAllowed)
            {
                ShowBanner();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[BannerAdManager] OnSceneLoaded: {scene.name}");
            HookGating();
            CheckCurrentScene();
            
            if (_bannerProvider == null)
            {
                Debug.LogWarning("[BannerAdManager] Banner provider not set when scene loaded. Waiting for provider initialization.");
                return;
            }
            
            if (enableBanners && _isCurrentSceneAllowed)
            {
                StartCoroutine(ShowBannerAfterDelay(0.1f));
            }
            else
            {
                HideBanner();
            }
        }

        private System.Collections.IEnumerator ShowBannerAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowBanner();
        }

        private void CheckCurrentScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            _isCurrentSceneAllowed = System.Array.Exists(scenesWithBanners, sceneName => sceneName == currentScene);
            
            Debug.Log($"[BannerAdManager] Scene: {currentScene}, Banner allowed: {_isCurrentSceneAllowed}");
        }

        public void SetBannerProvider(IBannerAdProvider provider)
        {
            if (_bannerProvider != null)
            {
                _bannerProvider.DestroyBanner();
            }

            _bannerProvider = provider;
            Debug.Log($"[BannerAdManager] Banner provider set: {provider.GetType().Name}");

            CheckCurrentScene();
            
            if (enableBanners && _isCurrentSceneAllowed)
            {
                Debug.Log($"[BannerAdManager] Auto-showing banner after provider set in scene: {SceneManager.GetActiveScene().name}");
                ShowBanner();
            }
        }

        public void ShowBanner()
        {
            if (!enableBanners)
            {
                Debug.Log("[BannerAdManager] Banners are disabled");
                return;
            }
            
            if (!_isCurrentSceneAllowed)
            {
                Debug.Log($"[BannerAdManager] Banner not allowed in scene: {SceneManager.GetActiveScene().name}");
                return;
            }

            if (_bannerProvider == null)
            {
                Debug.LogWarning("[BannerAdManager] No banner provider set!");
                return;
            }

            // VIP ou "remover anúncios": sem banner (antes o banner aparecia mesmo assim).
            if (!AdsAllowed())
            {
                Debug.Log("[BannerAdManager] Ads disabled for this player (VIP/remove ads)");
                _bannerProvider.HideBanner();
                return;
            }

            Debug.Log("[BannerAdManager] Showing banner ad");
            _bannerProvider.LoadAndShowBanner();
        }

        public void HideBanner()
        {
            if (_bannerProvider == null)
            {
                Debug.LogWarning("[BannerAdManager] No banner provider set!");
                return;
            }

            Debug.Log("[BannerAdManager] Hiding banner ad");
            _bannerProvider.HideBanner();
        }

        public void DestroyBanner()
        {
            if (_bannerProvider == null) return;

            Debug.Log("[BannerAdManager] Destroying banner ad");
            _bannerProvider.DestroyBanner();
        }

        public bool IsBannerShowing()
        {
            return _bannerProvider != null && _bannerProvider.IsBannerShowing();
        }

        public void EnableBanners(bool enable)
        {
            enableBanners = enable;
            
            if (!enable && _bannerProvider != null)
            {
                HideBanner();
            }
            else if (enable && _bannerProvider != null)
            {
                ShowBanner();
            }
        }

        private static bool AdsAllowed()
        {
            var gating = AdGatingService.Instance;
            return gating == null || gating.ShouldShowAds();
        }

        private void HookGating()
        {
            var gating = AdGatingService.Instance;
            if (gating == _gating) return;

            if (_gating != null)
                _gating.OnAdStatusChanged -= HandleAdStatusChanged;

            _gating = gating;

            if (_gating != null)
                _gating.OnAdStatusChanged += HandleAdStatusChanged;
        }

        private void HandleAdStatusChanged()
        {
            if (_bannerProvider == null) return;

            if (!AdsAllowed())
                _bannerProvider.HideBanner();
            else if (enableBanners && _isCurrentSceneAllowed)
                ShowBanner();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_gating != null)
                _gating.OnAdStatusChanged -= HandleAdStatusChanged;
            
            if (_bannerProvider != null)
            {
                _bannerProvider.DestroyBanner();
            }
        }
    }
}
