using Core.SaveSystem;
using DG.Tweening;
using Gameplay.Controllers;
using Gameplay.Utils;
using New_GameplayCore.Services;
using UI.Views;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menus
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button rankingButton;
        [SerializeField] private Button storeButton;
        [SerializeField] private Button dailyMissionsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button profileButton;
        
        [Header("LGPD")]
        [SerializeField] private LgpdView lgpdViewPrefab;
        [SerializeField] private Transform uiRoot;


        [Header("Title")]
        [SerializeField] private RectTransform titleTransform;

        [Header("Icons ao lado dos botões")]
        [SerializeField] private RectTransform[] sideIcons;

        [Header("Badges")]
        [SerializeField] private DailyMissionsController dailyController;
        [SerializeField] private GameObject dailyBadge;

        [Header("Settings (janela/painel)")]
        [SerializeField] private SettingsMenu settingsPanel;
        [SerializeField] private RectTransform settingsIcon;
    
        [Header("Ranking")]
        [SerializeField] private GameObject rankingPanel;

        [Header("Panels")]
        //[SerializeField] private ShopPanelController shopPanel;
        [SerializeField] private DailyMissionsPanelTabs dailyMissionsPanel;
        [SerializeField] private GameObject profilePanel;

        [Header("Animação de tilt")]
        [SerializeField] private float initialDelay = 1.5f;
        [SerializeField] private float repeatInterval = 3f;
        [SerializeField] private float tiltAngle = 12f;
        [SerializeField] private float tiltDuration = 0.18f;
        [SerializeField] private float returnDuration = 0.22f;
        [SerializeField] private float iconsStagger = 0.07f;

        private Sequence _titleSeq;
        private bool _switching;
        private LevelProgressService _progress;
        private bool _saveSystemInitialized;

        private void InitializeSaveSystem()
        {
            if (_saveSystemInitialized) return;
            
            if (Managers.PlayerDataManager.Instance != null)
            {
                Managers.PlayerDataManager.Instance.SetAutoSaveInterval(120f);
                Managers.PlayerDataManager.Instance.EnableAutoSave(true);
                Debug.Log("[MainMenu] ✅ Auto-save configured (120s interval)");
            }
            
            EnsureApplicationLifecycleManager();
            SaveHelper.LogSaveSystemStatus();
            
            _saveSystemInitialized = true;
        }

        private void EnsureApplicationLifecycleManager()
        {
            var existing = FindFirstObjectByType<Managers.ApplicationLifecycleManager>();
            if (existing == null)
            {
                var go = new GameObject("ApplicationLifecycleManager");
                go.AddComponent<Managers.ApplicationLifecycleManager>();
                Debug.Log("[MainMenu] ✅ ApplicationLifecycleManager created");
            }
        }

        private void Start()
        {
            InitializeSaveSystem();
            
            var profile = SaveManager.Instance.Load<PlayerProfileData>();

            if (!profile.hasAcceptedLGPD && lgpdViewPrefab != null)
            {
                var instance = Instantiate(lgpdViewPrefab, uiRoot != null ? uiRoot : transform);
            }

            _progress = new LevelProgressService();
            _progress.Load();

            titleTransform.localScale = Vector3.zero;
            _titleSeq = DOTween.Sequence()
                .Append(titleTransform.DOScale(1f, 0.8f).SetEase(Ease.OutBounce));
        
            playButton.onClick.AddListener(OnClickPlay);
            rankingButton.onClick.AddListener(() =>
            {
            if (!rankingPanel) return;
                rankingPanel.SetActive(true);
            });

            storeButton.onClick.AddListener(OpenStorePanel);
            profileButton.onClick.AddListener(OpenProfilePanel);
            dailyMissionsButton.onClick.AddListener(OpenDailyPanel);
        
            if (dailyController)
            {
                dailyController.OnAttentionChanged += (has) =>
                {
                    if (dailyBadge) dailyBadge.SetActive(has);
                };
                if (dailyBadge) dailyBadge.SetActive(dailyController.HasAnyClaimAvailable());
            }
        
            settingsButton.onClick.AddListener(() =>
            {
                if (!settingsPanel) return;
                Managers.AnalyticsManager.Instance?.TrackMenuOpened("settings");
                settingsPanel.Toggle();
            });
        
            InvokeRepeating(nameof(NudgeAllOnce), initialDelay, repeatInterval);
        }

        public void OnClickPlay()
        {
            var idx = Mathf.Max(_progress.CurrentIndex, _progress.UnlockedMaxIndex);

            _progress.SetCurrentIndex(idx);
            _progress.Save();
            
            var profileController = Object.FindFirstObjectByType<PlayerProfileController>();
            if (profileController != null)
            {
                profileController.SetCurrentLevel(idx);
            }
            
            SaveHelper.SaveToLocal();
            Debug.Log("[MainMenu] Progress saved before entering gameplay");
        
            Managers.AnalyticsManager.Instance?.TrackMenuOpened("gameplay");
        
            LoadingManager.LoadScene("Gameplay Scene");
        }

        private void OnDisable()
        {
            _titleSeq?.Kill();
            CancelInvoke(nameof(NudgeAllOnce));
            if (sideIcons != null)
            {
                foreach (var icon in sideIcons)
                {
                    if (!icon) continue;
                    icon.DOKill();
                    icon.localRotation = Quaternion.identity;
                    icon.localScale = Vector3.one;
                }
            }
            if (settingsIcon)
            {
                settingsIcon.DOKill();
                settingsIcon.localRotation = Quaternion.identity;
                settingsIcon.localScale = Vector3.one;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;
            CancelInvoke(nameof(NudgeAllOnce));
            InvokeRepeating(nameof(NudgeAllOnce), 0.25f, repeatInterval);
        }

        private void NudgeAllOnce()
        {
            if (sideIcons != null)
            {
                for (int i = 0; i < sideIcons.Length; i++)
                {
                    var icon = sideIcons[i];
                    if (!icon) continue;

                    var seq = DOTween.Sequence().SetDelay(i * iconsStagger);
                    seq.Append(icon.DOLocalRotate(new Vector3(0, 0, tiltAngle), tiltDuration).SetEase(Ease.OutQuad));
                    seq.Append(icon.DOLocalRotate(Vector3.zero, returnDuration).SetEase(Ease.InOutQuad));
                    seq.Join(icon.DOScale(1.03f, tiltDuration + returnDuration).SetEase(Ease.InOutSine))
                        .Append(icon.DOScale(1f, 0.08f));
                }
            }
        
            if (settingsIcon)
            {
                DOTween.Sequence()
                    .Append(settingsIcon.DOLocalRotate(new Vector3(0, 0, -tiltAngle), tiltDuration).SetEase(Ease.OutQuad))
                    .Append(settingsIcon.DOLocalRotate(Vector3.zero, returnDuration).SetEase(Ease.InOutQuad));
            }
            else if (settingsButton)
            {
                var t = settingsButton.transform;
                DOTween.Sequence()
                    .Append(t.DOLocalRotate(new Vector3(0, 0, -tiltAngle), tiltDuration).SetEase(Ease.OutQuad))
                    .Append(t.DOLocalRotate(Vector3.zero, returnDuration).SetEase(Ease.InOutQuad));
            }
        }

        private void OpenStorePanel()
        {
            if (_switching) return;
            _switching = true;
        
            Managers.AnalyticsManager.Instance?.TrackStoreOpened();
        
            if (dailyMissionsPanel && dailyMissionsPanel.gameObject.activeInHierarchy)
                dailyMissionsPanel.Hide();

            //if (shopPanel) shopPanel.Show();

            DOVirtual.DelayedCall(0.05f, () => _switching = false);
        }

        private void OpenDailyPanel()
        {
            if (_switching) return;
            _switching = true;

            //if (shopPanel && shopPanel.gameObject.activeInHierarchy)
            //shopPanel.Hide();

            if (dailyMissionsPanel) dailyMissionsPanel.Show();

            DOVirtual.DelayedCall(0.05f, () => _switching = false);
        }
        
        public void OpenProfilePanel()
        {
            if(profilePanel != null)
                profilePanel.SetActive(true);
        }
    }
}
