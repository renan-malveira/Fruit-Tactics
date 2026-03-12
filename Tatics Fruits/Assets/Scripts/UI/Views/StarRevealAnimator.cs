using System;
using System.Collections;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class StarRevealAnimator : MonoBehaviour
    {
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1f, 1f);
        [SerializeField] private AnimationCurve fillCurve = AnimationCurve.EaseInOut(0, 0, 1f, 1f);
        [SerializeField] private float punchScale = 1.3f;
        [SerializeField] private float punchDuration = 0.25f;
        [SerializeField] private float settleDuration = 0.15f;

        private static readonly Color DimColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        public void RevealSequence(Image[] stars, bool[] earned, float delayBetween, Action onComplete = null)
        {
            StartCoroutine(SequenceRoutine(stars, earned, delayBetween, onComplete));
        }

        private IEnumerator SequenceRoutine(Image[] stars, bool[] earned, float delayBetween, Action onComplete)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] != null)
                    StartCoroutine(RevealOneStar(stars[i], earned[i]));
                
                yield return new WaitForSeconds(delayBetween);
            }
            
            yield return new WaitForSeconds(punchDuration + settleDuration);
            onComplete?.Invoke();
        }

        private IEnumerator RevealOneStar(Image img, bool earned)
        {
            img.enabled = true;
            img.color = earned ? Color.white :  DimColor;

            var t = img.transform;
            var originScale = Vector3.one;
            var targetScale = Vector3.one * punchScale;
            var elapsed = 0f;

            while (elapsed < punchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                    var p = Mathf.Clamp01(elapsed / punchDuration);
                    t.localScale = Vector3.LerpUnclamped(originScale, targetScale, scaleCurve.Evaluate(p));
                    yield return null;
            }
            
            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var p = Mathf.Clamp01(elapsed / settleDuration);
                t.localScale = Vector3.LerpUnclamped(originScale, targetScale, fillCurve.Evaluate(p));
                yield return null;
            }
            
            t.localScale = originScale;
        }
    }
}