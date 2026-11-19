using Core.Services;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using New_GameplayCore;

namespace New_GameplayCore.Views
{
    public class GameplaySettings : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button openSettingsButton;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button musicButton;
        [SerializeField] private Button sfxButton;
        [SerializeField] private Button quitButton;

        [Header("Sprites")]
        [SerializeField] private Sprite musicOnSprite;
        [SerializeField] private Sprite musicOffSprite;
        [SerializeField] private Sprite sfxOnSprite;
        [SerializeField] private Sprite sfxOffSprite;

        [Header("Quit Popup")]
        [SerializeField] private GameObject quitPopupPrefab;
        [SerializeField] private Transform canvasTransform;

        private ITimeManager _timeManager;
        private GameSettingsModel _settings;

        private bool _isMusicEnabled = true;
        private bool _isSfxEnabled = true;
        private Image _musicButtonImage;
        private Image _sfxButtonImage;

        private void Awake()
        {
            LoadSettings();
            InitializeComponents();
        }

        public void Initialize(ITimeManager timeManager)
        {
            _timeManager = timeManager;
        }

        private void Start()
        {
            SetupButtonListeners();
            
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            UpdateButtonVisuals();
        }

        private void InitializeComponents()
        {
            if (musicButton != null)
            {
                _musicButtonImage = musicButton.GetComponent<Image>();
            }

            if (sfxButton != null)
            {
                _sfxButtonImage = sfxButton.GetComponent<Image>();
            }
        }

        private void SetupButtonListeners()
        {
            if (openSettingsButton != null)
            {
                openSettingsButton.onClick.AddListener(ToggleSettingsMenu);
            }

            if (musicButton != null)
            {
                musicButton.onClick.AddListener(ToggleMusic);
            }

            if (sfxButton != null)
            {
                sfxButton.onClick.AddListener(ToggleSFX);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitToMainMenu);
            }
        }

        private void ToggleSettingsMenu()
        {
            if (settingsPanel != null)
            {
                bool isOpening = !settingsPanel.activeSelf;
                settingsPanel.SetActive(isOpening);

                if (_timeManager != null)
                {
                    if (isOpening)
                    {
                        _timeManager.Pause();
                    }
                    else
                    {
                        _timeManager.Resume();
                    }
                }
            }
        }

        private void ToggleMusic()
        {
            _isMusicEnabled = !_isMusicEnabled;
            SaveSettings();
            UpdateButtonVisuals();
        }

        private void ToggleSFX()
        {
            _isSfxEnabled = !_isSfxEnabled;
            SaveSettings();
            UpdateButtonVisuals();
        }

        private void UpdateButtonVisuals()
        {
            if (_musicButtonImage != null)
            {
                _musicButtonImage.sprite = _isMusicEnabled ? musicOnSprite : musicOffSprite;
            }

            if (_sfxButtonImage != null)
            {
                _sfxButtonImage.sprite = _isSfxEnabled ? sfxOnSprite : sfxOffSprite;
            }
        }

        private void QuitToMainMenu()
        {
            ShowQuitPopup();
        }

        private void ShowQuitPopup()
        {
            if (quitPopupPrefab != null)
            {
                Transform parentTransform = canvasTransform != null ? canvasTransform : transform.parent;
                GameObject popup = Instantiate(quitPopupPrefab, parentTransform);

                if (_timeManager != null)
                {
                    _timeManager.Pause();
                }

                QuitPopup quitPopupComponent = popup.GetComponent<QuitPopup>();
                if (quitPopupComponent != null)
                {
                    quitPopupComponent.SetTimeManager(_timeManager);
                    quitPopupComponent.SetGameplaySettings(this);
                }
            }
        }

        /// <summary>
        /// Botão Voltar na partida: fecha o popup de sair (e continua), fecha a pausa
        /// ou abre a pausa. Chamado pelo Core.BackButtonRouter.
        /// </summary>
        public bool HandleBack()
        {
            var popup = FindFirstObjectByType<QuitPopup>();
            if (popup != null)
            {
                popup.Continue();
                return true;
            }

            if (settingsPanel == null)
                return false;

            ToggleSettingsMenu();
            return true;
        }

        public void CloseSettings()
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void LoadSettings()
        {
            _settings = SettingsRepository.Get();
            _isMusicEnabled = _settings.musicOn;
            _isSfxEnabled = _settings.sfxOn;
        }

        private void SaveSettings()
        {
            _settings.musicOn = _isMusicEnabled;
            _settings.sfxOn = _isSfxEnabled;
            SettingsRepository.Save(_settings);
        }

        private void OnDestroy()
        {
            if (openSettingsButton != null)
            {
                openSettingsButton.onClick.RemoveListener(ToggleSettingsMenu);
            }

            if (musicButton != null)
            {
                musicButton.onClick.RemoveListener(ToggleMusic);
            }

            if (sfxButton != null)
            {
                sfxButton.onClick.RemoveListener(ToggleSFX);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitToMainMenu);
            }
        }
    }
}
