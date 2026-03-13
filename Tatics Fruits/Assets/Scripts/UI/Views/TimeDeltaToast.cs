using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI.Views
{
    public class TimeDeltaToast : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private CanvasGroup cg;
        [SerializeField] private float rise            = 80f;
        [SerializeField] private float duration        = 0.8f;
        [SerializeField] private Color plusColor       = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color minusColor      = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private float penaltyMinScale = 1.3f;
        [SerializeField] private float penaltyMaxScale = 1.8f;
        [SerializeField] private int   penaltyMaxValue = 7;
        [SerializeField] private float shakeStrength   = 8f;

        public void Play(int delta)
        {
            if (!label) label = GetComponentInChildren<TextMeshProUGUI>();
            if (!cg) cg = GetComponentInChildren<CanvasGroup>();

            var sign = delta > 0 ? "+" : "";
            label.text = $"{sign}{delta}s";
            label.color = delta >= 0 ? plusColor : minusColor;

            var rt = (RectTransform)transform;
            var start = rt.anchoredPosition;

            cg.alpha        = 0f;
            rt.localScale   = Vector3.one;
            DOTween.Kill(this);

            if (delta < 0)
                PlayPenalty(rt, start, delta);
            else
                PlayBonus(rt, start);
        }

        private void PlayBonus(RectTransform rt, Vector2 start)
        {
            var s = DOTween.Sequence().SetTarget(this);
            s.Append(cg.DOFade(1f, 0.12f));
            s.Join(rt.DOScale(1.05f, 0.12f).SetEase(Ease.OutBack));
            s.Append(rt.DOAnchorPos(start + new Vector2(0f, rise), duration).SetEase(Ease.OutQuad));
            s.Join(cg.DOFade(0f, duration));
            s.OnComplete(() => Destroy(gameObject));
        }

        private void PlayPenalty(RectTransform rt, Vector2 start, int delta)
        {
            var t= Mathf.Clamp01(Mathf.Abs(delta) / (float)penaltyMaxValue);
            var peakScale = Mathf.Lerp(penaltyMinScale, penaltyMaxScale, t);
            var drop= rise * 0.4f;

            var s = DOTween.Sequence().SetTarget(this);
            s.Append(cg.DOFade(1f, 0.08f));
            s.Join(rt.DOScale(peakScale, 0.1f).SetEase(Ease.OutBack));
            s.AppendCallback(() => rt.DOShakeAnchorPos(0.15f, shakeStrength, 20, 0f));
            s.Append(rt.DOScale(1f, 0.1f).SetEase(Ease.InQuad));
            s.Append(rt.DOAnchorPos(start - new Vector2(0f, drop), duration * 0.9f).SetEase(Ease.OutQuad));
            s.Join(cg.DOFade(0f, duration * 0.9f));
            s.OnComplete(() => Destroy(gameObject));
        }
    }
}
