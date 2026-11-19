using Core.Services;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace New_GameplayCore.Views
{
    public class QuitPopup : MonoBehaviour
    {
        [Header("Button References")]
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button continueButton;

        [Header("Scene Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private ITimeManager _timeManager;
        private GameplaySettings _gameplaySettings;

        private void Start()
        {
            SetupButtonListeners();
        }

        public void SetTimeManager(ITimeManager timeManager)
        {
            _timeManager = timeManager;
        }

        public void SetGameplaySettings(GameplaySettings gameplaySettings)
        {
            _gameplaySettings = gameplaySettings;
        }

        private void SetupButtonListeners()
        {
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(LoadMainMenu);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(ContinueGameplay);
            }
        }

        private void LoadMainMenu()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        /// <summary>Mesmo efeito do botão Continuar (usado pelo botão Voltar do Android).</summary>
        public void Continue() => ContinueGameplay();

        private void ContinueGameplay()
        {
            if (_timeManager != null)
            {
                _timeManager.Resume();
            }

            if (_gameplaySettings != null)
            {
                _gameplaySettings.CloseSettings();
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(LoadMainMenu);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(ContinueGameplay);
            }
        }
    }
}
