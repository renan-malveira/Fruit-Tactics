using System.Collections;
using Core.Services;
using Gameplay.Utils;
using New_GameplayCore.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class DefeatView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image blocker;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Image star1;
        [SerializeField] private Image star2;
        [SerializeField] private Image star3;
        [SerializeField] private Sprite starOn;
        [SerializeField] private Sprite starOff;

        [Header("Animation")]
        [SerializeField] private StarRevealAnimator starAnimator;
        [SerializeField] private float panelFadeInDuration = 0.3f;
        [SerializeField] private float starRevealDelay     = 0.15f;
        [SerializeField] private float starStaggerDelay    = 0.3f;

        [Header("Buttons")]
        [SerializeField] private Button replayButton;
        [SerializeField] private Button menuButton;

        private DefeatPresenter _presenter;

        private const float PanelStartAlpha = 0f;
        private const float PanelEndAlpha   = 1f;

        public void Bind(DefeatPresenter presenter, DefeatModel model)
        {
            _presenter = presenter;

            if (titleText)
                titleText.text = Localizer.Instance.Tr("defeat_title_timeup", "Tempo esgotado! Não foi dessa vez.");

            if (scoreText)
                scoreText.text = Localizer.Instance.TrFormat("defeat_score_line", "Você fez: {0} pontos.", model.totalScore);

            AssignStarSprites(model.starsEarned);
            SetButtonsInteractable(false);

            if (replayButton)
            {
                replayButton.onClick.RemoveAllListeners();
                replayButton.onClick.AddListener(() =>
                {
                    Managers.AnalyticsManager.Instance?.TrackButtonClicked("defeat_replay");
                    _presenter.ClickReplay();
                });
            }
            if (menuButton)
            {
                menuButton.onClick.RemoveAllListeners();
                menuButton.onClick.AddListener(() =>
                {
                    Managers.AnalyticsManager.Instance?.TrackButtonClicked("defeat_menu");
                    _presenter.ClickMenu();
                });
            }

            Show();
            StartCoroutine(PlayEntrance(model));
        }

        private void AssignStarSprites(int starsEarned)
        {
            AssignSprite(star1, starsEarned >= 1);
            AssignSprite(star2, starsEarned >= 2);
            AssignSprite(star3, starsEarned >= 3);
        }

        private void AssignSprite(Image img, bool on)
        {
            if (!img) return;
            img.sprite  = on ? starOn : starOff;
            img.enabled = false;
        }

        private IEnumerator PlayEntrance(DefeatModel model)
        {
            yield return StartCoroutine(FadePanel(PanelStartAlpha, PanelEndAlpha, panelFadeInDuration));
            yield return new WaitForSeconds(starRevealDelay);

            var images = new[] { star1, star2, star3 };
            var earned = new[]
            {
                model.starsEarned >= 1,
                model.starsEarned >= 2,
                model.starsEarned >= 3
            };

            if (starAnimator != null)
                starAnimator.RevealSequence(images, earned, starStaggerDelay, () => SetButtonsInteractable(true));
            else
            {
                foreach (var img in images) if (img) img.enabled = true;
                SetButtonsInteractable(true);
            }
        }

        private IEnumerator FadePanel(float from, float to, float duration)
        {
            if (!canvasGroup) yield break;

            canvasGroup.alpha = from;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed           += Time.unscaledDeltaTime;
                canvasGroup.alpha  = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            canvasGroup.alpha = to;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (replayButton) replayButton.interactable = interactable;
            if (menuButton)   menuButton.interactable   = interactable;
        }

        private void Show()
        {
            gameObject.SetActive(true);
            if (canvasGroup)
            {
                canvasGroup.alpha          = 0f;
                canvasGroup.interactable   = true;
                canvasGroup.blocksRaycasts = true;
            }
            if (blocker) blocker.raycastTarget = true;
        }
    }
}
