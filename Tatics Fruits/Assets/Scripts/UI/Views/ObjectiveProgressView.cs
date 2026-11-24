using Core.ScriptableObjects;
using Core.Services;
using DG.Tweening;
using MoreMountains.Feedbacks;
using New_GameplayCore;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    /// <summary>
    /// Barra da meta e as três cestas de estrela do topo, pela pontuação (StarRules):
    /// a cesta 1 fica em 1/3 da barra, a 2 em 2/3 e a 3 no fim. Cada cesta começa apagada
    /// e acende quando a barra chega nela; é a mesma conta do popup de vitória.
    /// </summary>
    public class ObjectiveProgressView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image progressFill;
        [SerializeField] private Image star1;
        [SerializeField] private Image star2;
        [SerializeField] private Image star3;

        [Header("Refs")]
        [SerializeField] private GameControllerInitializer bootstrap;

        [Header("Feel/VFX")]
        [SerializeField] private MMF_Player starFeel1;
        [SerializeField] private MMF_Player starFeel2;
        [SerializeField] private MMF_Player starFeel3;

        [Header("Bar progress feel")]
        [SerializeField] private MMF_Player progressFeel;
        [SerializeField] private float tickMinDelta = 0.2f;

        [Header("Posição das cestas")]
        [Tooltip("Coloca as cestas 1 e 2 no ponto da barra em que acendem (1/3 e 2/3). A 3 fica no fim, onde está na cena.")]
        [SerializeField] private bool alignStarsToThresholds = true;

        private static readonly Color DimColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private IScoreService _score;
        private LevelConfigSo _cfg;

        private readonly bool[] _lit = new bool[3];
        private int _shownStars = -1;
        private Image _pulsing;

        private void Start()
        {
            if (bootstrap.IsReady)
            {
                Initialize();
            }
            else
            {
                bootstrap.OnReady += Initialize;
            }
        }

        private void Initialize()
        {
            _score = bootstrap.Score;
            _cfg = bootstrap.LevelConfig;

            if (alignStarsToThresholds)
                AlignStars();

            Refresh(_score.Total, 0);
            _score.OnScoreChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (bootstrap != null)
                bootstrap.OnReady -= Initialize;

            if (_score != null)
                _score.OnScoreChanged -= Refresh;

            KillTweens(star1);
            KillTweens(star2);
            KillTweens(star3);
        }

        private void Refresh(int total, int delta)
        {
            var target = Mathf.Max(1, _cfg.targetScore);
            var pct = Mathf.Clamp01(total / (float)target);

            if (delta > 0)
                progressFeel?.PlayFeedbacks();

            if (progressFill)
                progressFill.DOFillAmount(pct, 0.25f);

            RefreshStars(StarRules.Evaluate(_cfg, total));
        }

        private void RefreshStars(int stars)
        {
            if (stars == _shownStars)
                return;

            var first = _shownStars < 0;
            _shownStars = stars;

            SetPulse(stars >= 3 ? star3 : stars == 2 ? star2 : stars == 1 ? star1 : null);

            SetStar(0, star1, starFeel1, stars >= 1, first);
            SetStar(1, star2, starFeel2, stars >= 2, first);
            SetStar(2, star3, starFeel3, stars >= 3, first);
        }

        private void SetStar(int index, Image star, MMF_Player feel, bool lit, bool instant)
        {
            if (!star) return;

            var changed = _lit[index] != lit;
            _lit[index] = lit;

            star.enabled = true;
            var color = lit ? Color.white : DimColor;

            if (instant || !changed)
            {
                if (instant)
                {
                    star.DOKill();
                    star.color = color;
                }
                return;
            }

            star.DOKill();
            star.DOColor(color, 0.2f);

            if (!lit)
                return;

            feel?.PlayFeedbacks();

            // "Pop" ao acender; se for a cesta mais alta, volta a pulsar depois do pop.
            star.transform.DOKill();
            star.transform.localScale = Vector3.one;
            star.transform
                .DOPunchScale(Vector3.one * 0.25f, 0.35f, 8, 0.6f)
                .OnComplete(() =>
                {
                    if (star && star == _pulsing)
                        StartPulse(star);
                });
        }

        /// <summary>A cesta mais alta já ganha fica pulsando.</summary>
        private void SetPulse(Image target)
        {
            if (target == _pulsing)
                return;

            if (_pulsing)
            {
                DOTween.Kill($"pulse_{_pulsing.GetInstanceID()}");
                _pulsing.transform.DOKill();
                _pulsing.transform.localScale = Vector3.one;
            }

            _pulsing = target;
            if (target)
                StartPulse(target);
        }

        private static void StartPulse(Image target)
        {
            target.transform.DOKill();
            target.transform.localScale = Vector3.one;
            target.transform
                .DOScale(1.05f, 0.6f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetId($"pulse_{target.GetInstanceID()}");
        }

        /// <summary>
        /// As cestas 1 e 2 estavam em ~25% e ~50% da barra. Com as estrelas em 1/3 e 2/3,
        /// elas vão para o ponto exato em que acendem.
        /// </summary>
        private void AlignStars()
        {
            if (!progressFill)
                return;

            Canvas.ForceUpdateCanvases();

            var corners = new Vector3[4];
            progressFill.rectTransform.GetWorldCorners(corners);
            var left = corners[0];
            var right = corners[3];

            PlaceOnBar(star1, left, right, StarRules.FractionFor(_cfg, 1));
            PlaceOnBar(star2, left, right, StarRules.FractionFor(_cfg, 2));
        }

        private static void PlaceOnBar(Image star, Vector3 left, Vector3 right, float fraction)
        {
            if (!star) return;

            var t = star.transform;
            var onBar = Vector3.Lerp(left, right, Mathf.Clamp01(fraction));
            t.position = new Vector3(onBar.x, t.position.y, t.position.z);
        }

        private static void KillTweens(Image star)
        {
            if (!star) return;
            star.DOKill();
            star.transform.DOKill();
        }
    }
}
