using System;
using System.Collections;
using Core.ScriptableObjects;
using Core.Services;
using DG.Tweening;
using Gameplay.Utils;
using New_GameplayCore;
using New_GameplayCore.Services;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Views
{
    public class PreRoundView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image fadeBlocker;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private TextMeshProUGUI bestScoreText;

        [Header("Stars")]
        [SerializeField] private Image star1;
        [SerializeField] private Image star2;
        [SerializeField] private Image star3;
        [SerializeField] private TextMeshProUGUI star1ScoreLabel;
        [SerializeField] private TextMeshProUGUI star2ScoreLabel;
        [SerializeField] private TextMeshProUGUI star3ScoreLabel;
        [SerializeField] private Color starOnColor  = Color.yellow;
        [SerializeField] private Color starOffColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private float starRevealDelay = 0.08f;

        [Header("Buttons")]
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button nextButton;

        [Header("Countdown")]
        private CountdownView _countdown;

        private IPreRoundPresenter _presenter;
        private PreRoundModel _model;

        public void Bind(IPreRoundPresenter presenter, PreRoundModel model)
        {
            _presenter = presenter;
            _model     = model;

            if (titleText)
            {
                titleText.text = model.levelNumber > 0
                    ? Localizer.Instance.TrFormat("level_title", "Fase {0}", model.levelNumber)
                    : model.displayName;
            }

            if (objectiveText)
            {
                objectiveText.text = Localizer.Instance.TrFormat(
                    "pre_round_objective",
                    "Faça <b>{0}</b> pontos antes de o tempo acabar! Você começa com <b>{1}</b> segundos.",
                    model.targetScore.ToString("N0"),
                    model.initialTimeSec);
            }

            if (bestScoreText)
            {
                bestScoreText.gameObject.SetActive(model.bestScore > 0);
                if (model.bestScore > 0)
                    bestScoreText.text = Localizer.Instance.TrFormat(
                        "pre_round_best",
                        "Melhor: {0}",
                        model.bestScore);
            }

            // Cada estrela mostra o seu critério: meta de pontos, tempo-alvo e "sem erros".
            SetStarLabel(star1ScoreLabel, model.star1Score.ToString("N0"));
            SetStarLabel(star2ScoreLabel, model.parTimeSeconds > 0
                ? Localizer.Instance.TrFormat("pre_round_star_time", "Até {0}s", model.star2Score)
                : Localizer.Instance.TrFormat("pre_round_star_time_left", "{0}s sobrando", model.star2Score));
            SetStarLabel(star3ScoreLabel, Localizer.Instance.Tr("pre_round_star_perfect", "Sem erros"));

            SetStarImmediate(star1, model.bestStars >= 1);
            SetStarImmediate(star2, model.bestStars >= 2);
            SetStarImmediate(star3, model.bestStars >= 3);

            mainMenuButton?.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

            if (nextButton)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnStart);
            }

            gameObject.SetActive(true);
            StartCoroutine(EnterSequence());
        }

        public void SetupCountdown(CountdownView countdown)
        {
            _countdown = countdown;
        }

        private IEnumerator EnterSequence()
        {
            yield return FadeCanvas(0f, 1f, 0.2f);
            yield return RevealStars();
        }

        private IEnumerator RevealStars()
        {
            yield return AnimateStar(star1, _model.bestStars >= 1);
            yield return new WaitForSecondsRealtime(starRevealDelay);
            yield return AnimateStar(star2, _model.bestStars >= 2);
            yield return new WaitForSecondsRealtime(starRevealDelay);
            yield return AnimateStar(star3, _model.bestStars >= 3);
        }

        private IEnumerator AnimateStar(Image img, bool achieved)
        {
            if (!img) yield break;

            img.color = achieved ? starOnColor : starOffColor;
            img.transform.localScale = Vector3.zero;

            var tween = img.transform
                .DOScale(achieved ? 1.25f : 1f, 0.25f)
                .SetEase(achieved ? Ease.OutBack : Ease.OutQuad);

            if (achieved)
            {
                tween.OnComplete(() =>
                    img.transform.DOScale(1f, 0.12f).SetEase(Ease.InQuad));
            }

            yield return tween.WaitForCompletion();
        }

        private static void SetStarLabel(TextMeshProUGUI label, string text)
        {
            if (!label) return;

            // Os rótulos eram só números; textos como "Sem erros" precisam caber no mesmo espaço.
            if (!label.enableAutoSizing)
            {
                label.fontSizeMax = label.fontSize;
                label.fontSizeMin = Mathf.Max(10f, label.fontSize * 0.5f);
                label.enableAutoSizing = true;
            }
            label.text = text;
        }

        private void SetStarImmediate(Image img, bool achieved)
        {
            if (!img) return;
            img.color = achieved ? starOnColor : starOffColor;
            img.transform.localScale = Vector3.zero;
        }

        private void OnStart()
        {
            StartCoroutine(CloseThen(_presenter.OnStartClicked));
        }

        private IEnumerator CloseThen(Action callback)
        {
            yield return FadeCanvas(1f, 0f, 0.15f);

            if (_countdown != null)
                yield return _countdown.PlayCountdown();

            callback?.Invoke();
            Destroy(gameObject);
        }

        private IEnumerator FadeCanvas(float from, float to, float dur)
        {
            if (!canvasGroup) yield break;

            canvasGroup.alpha = from;
            if (fadeBlocker) fadeBlocker.raycastTarget = true;

            var t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }

            canvasGroup.alpha = to;
            if (fadeBlocker) fadeBlocker.raycastTarget = to > 0.99f;
        }

        private void OnDestroy()
        {
            DOTween.Kill(star1?.transform);
            DOTween.Kill(star2?.transform);
            DOTween.Kill(star3?.transform);
        }
    }
}
