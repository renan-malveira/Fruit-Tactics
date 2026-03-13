using System.Collections;
using Core.Services;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay.Utils;
using New_GameplayCore.Services;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Views
{
    public class VictoryView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image blocker;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI recordText;
        [SerializeField] private Image star1;
        [SerializeField] private Image star2;
        [SerializeField] private Image star3;
        [SerializeField] private Sprite starOn;
        [SerializeField] private Sprite starOff;

        [Header("Gold Reward")]
        [SerializeField] private GameObject goldRewardRoot;
        [SerializeField] private TextMeshProUGUI goldRewardText;
        [SerializeField] private RectTransform goldRewardRect;
        [SerializeField] private float goldRollupDuration = 0.8f;
        [SerializeField] private float goldRevealDelay    = 0.25f;

        [Header("Animation")]
        [SerializeField] private StarRevealAnimator starAnimator;
        [SerializeField] private float panelFadeInDuration = 0.3f;
        [SerializeField] private float starRevealDelay     = 0.2f;
        [SerializeField] private float starStaggerDelay    = 0.35f;
        [SerializeField] private float scoreCountDuration  = 1.2f;

        [Header("Buttons")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button menuButton;

        private VictoryPresenter _presenter;

        private const float PanelStartAlpha = 0f;
        private const float PanelEndAlpha   = 1f;

        public void Bind(VictoryPresenter presenter, VictoryModel model)
        {
            _presenter = presenter;

            if (titleText)
                titleText.text = Localizer.Instance.Tr("victory_title", "Vitória!");

            if (scoreText)
                scoreText.text = "0";

            if (recordText)
            {
                recordText.text = model.newRecord
                    ? Localizer.Instance.Tr("victory_new_record", "Novo Recorde!")
                    : Localizer.Instance.TrFormat("victory_previous_record", "{0}", model.bestBefore);
            }

            if (goldRewardRoot)
                goldRewardRoot.SetActive(false);

            AssignStarSprites(model.starsEarned);
            SetButtonsInteractable(false);

            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() =>
            {
                Managers.AnalyticsManager.Instance?.TrackButtonClicked("victory_next");
                _presenter.ClickNext();
            });
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() =>
            {
                Managers.AnalyticsManager.Instance?.TrackButtonClicked("victory_replay");
                _presenter.ClickReplay();
            });
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(() =>
            {
                Managers.AnalyticsManager.Instance?.TrackButtonClicked("victory_menu");
                SceneManager.LoadScene("MainMenu");
            });

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

        private IEnumerator PlayEntrance(VictoryModel model)
        {
            yield return StartCoroutine(FadePanel(PanelStartAlpha, PanelEndAlpha, panelFadeInDuration));
            yield return StartCoroutine(CountScore(model.totalScore));
            yield return new WaitForSeconds(starRevealDelay);

            var images = new[] { star1, star2, star3 };
            var earned = new[]
            {
                model.starsEarned >= 1,
                model.starsEarned >= 2,
                model.starsEarned >= 3
            };

            if (starAnimator != null)
                starAnimator.RevealSequence(images, earned, starStaggerDelay,
                    () => StartCoroutine(ShowGoldReward(model)));
            else
            {
                foreach (var img in images) if (img) img.enabled = true;
                StartCoroutine(ShowGoldReward(model));
            }
        }

        private IEnumerator ShowGoldReward(VictoryModel model)
        {
            if (model.goldEarned <= 0)
            {
                SetButtonsInteractable(true);
                yield break;
            }

            yield return new WaitForSeconds(goldRevealDelay);

            if (goldRewardRoot)
            {
                goldRewardRoot.SetActive(true);
                goldRewardRoot.transform.localScale = Vector3.zero;
                goldRewardRoot.transform
                    .DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBack);
            }

            if (goldRewardText)
            {
                var elapsed = 0f;
                while (elapsed < goldRollupDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var display = Mathf.RoundToInt(
                        Mathf.Lerp(0, model.goldEarned, Mathf.Clamp01(elapsed / goldRollupDuration)));
                    goldRewardText.text = $"+{display}";
                    yield return null;
                }
                goldRewardText.text = $"+{model.goldEarned}";
            }

            if (CoinCollectFx.Instance != null && goldRewardRect != null)
            {
                yield return UniTask.ToCoroutine(() =>
                    CoinCollectFx.Instance.PlayFromUI(goldRewardRect, model.goldEarned));
            }

            SetButtonsInteractable(true);
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

        private IEnumerator CountScore(int targetScore)
        {
            if (!scoreText) yield break;

            var elapsed = 0f;
            while (elapsed < scoreCountDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var display = Mathf.RoundToInt(
                    Mathf.Lerp(0, targetScore, Mathf.Clamp01(elapsed / scoreCountDuration)));
                scoreText.text = Localizer.Instance.TrFormat(
                    "victory_score_line", "Você fez: {0} pontos!", display);
                yield return null;
            }
            scoreText.text = Localizer.Instance.TrFormat(
                "victory_score_line", "Você fez: {0} pontos!", targetScore);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (nextButton)   nextButton.interactable   = interactable;
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
