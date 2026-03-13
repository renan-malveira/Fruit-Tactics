using System;
using DG.Tweening;
using Gameplay.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions.FantasyRPG;

namespace UI.Views
{
    public struct AllLevelsCompletedModel
    {
        public int TotalLevels;
        public int TotalStars;
        public int MaxStarsPossible;
        public int FinalScore;
    }

    public class AllLevelsCompletedView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI starsText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button leaderboardButton;

        [Header("Effects")]
        [SerializeField] private ParticleSystem[] celebrationParticles;
        [SerializeField] private UIParticleSystem[] uiCelebrationParticles;
        [SerializeField] private RectTransform panelTransform;
        [SerializeField] private float panelAnimationDuration = 0.8f;
        [SerializeField] private float particleDelay = 0.3f;
        [SerializeField] private float statsRevealDelay = 0.6f;

        public void Initialize(AllLevelsCompletedModel model, Action onPlayAgain, Action onMenuClick, Action onLeaderboard = null)
        {
            if (titleText)
                titleText.text = Localizer.Instance.Tr("levels_completed_title");

            if (messageText)
                messageText.text = Localizer.Instance.Tr("levels_completed_subtitle");

            if (starsText)
                starsText.text = $"{model.TotalStars} / {model.MaxStarsPossible}";

            if (scoreText)
                scoreText.text = model.FinalScore.ToString("N0");

            playAgainButton?.onClick.RemoveAllListeners();
            playAgainButton?.onClick.AddListener(() =>
            {
                Managers.AnalyticsManager.Instance?.TrackButtonClicked("all_levels_play_again");
                onPlayAgain?.Invoke();
            });

            menuButton?.onClick.RemoveAllListeners();
            menuButton?.onClick.AddListener(() =>
            {
                Managers.AnalyticsManager.Instance?.TrackButtonClicked("all_levels_completed_menu");
                onMenuClick?.Invoke();
            });

            leaderboardButton?.onClick.RemoveAllListeners();
            if (leaderboardButton != null)
            {
                leaderboardButton.gameObject.SetActive(onLeaderboard != null);
                leaderboardButton.onClick.AddListener(() =>
                {
                    Managers.AnalyticsManager.Instance?.TrackButtonClicked("all_levels_leaderboard");
                    onLeaderboard?.Invoke();
                });
            }

            Show();
        }

        private void Show()
        {
            gameObject.SetActive(true);

            if (canvasGroup)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, panelAnimationDuration).SetEase(Ease.OutQuad);
            }

            if (starsText) starsText.transform.localScale = Vector3.zero;
            if (scoreText) scoreText.transform.localScale = Vector3.zero;

            if (panelTransform)
            {
                panelTransform.localScale = Vector3.zero;
                panelTransform.DOScale(1f, panelAnimationDuration)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        PlayParticles();
                        RevealStats();
                    });
            }
            else
            {
                DOVirtual.DelayedCall(particleDelay, PlayParticles);
                DOVirtual.DelayedCall(statsRevealDelay, RevealStats);
            }
        }

        private void RevealStats()
        {
            starsText?.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
            scoreText?.transform
                .DOScale(1f, 0.35f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.1f);
        }

        private void PlayParticles()
        {
            if (celebrationParticles != null)
                foreach (var ps in celebrationParticles)
                    ps?.Play();

            if (uiCelebrationParticles != null)
                foreach (var uiPs in uiCelebrationParticles)
                    uiPs?.StartParticleEmission();
        }

        private void OnDisable()
        {
            canvasGroup?.DOKill();
            panelTransform?.DOKill();
            starsText?.transform.DOKill();
            scoreText?.transform.DOKill();
        }
    }
}
