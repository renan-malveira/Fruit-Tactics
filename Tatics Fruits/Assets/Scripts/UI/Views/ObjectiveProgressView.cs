using Core.ScriptableObjects;
using Core.Services;
using DG.Tweening;
using MoreMountains.Feedbacks;
using New_GameplayCore;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
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

        private IScoreService _score;
        private LevelConfigSo _cfg;
        private float _lastPct = -1f;

        private bool _star1Shown, _star2Shown, _star3Shown;

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
            
            Refresh(_score.Total, 0);
            _score.OnScoreChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (bootstrap != null)
                bootstrap.OnReady -= Initialize;
                
            if (_score != null)
                _score.OnScoreChanged -= Refresh;
        }

        private void Refresh(int total, int delta)
        {
            var target = Mathf.Max(1, _cfg.targetScore);
            var pct = Mathf.Clamp01(total / (float)target);

            if (delta > 0)
                progressFeel?.PlayFeedbacks();


            if (progressFill)
                progressFill.DOFillAmount(pct, 0.25f);
            
            HandleStar(star1, ref _star1Shown, pct >= _cfg.star1Threshold);
            HandleStar(star2, ref _star2Shown, pct >= _cfg.star2Threshold);
            HandleStar(star3, ref _star3Shown, pct >= _cfg.star3Threshold);
            
            if (_star3Shown)
                StopPulseExcept(star3);
            else if (_star2Shown)
                StopPulseExcept(star2);
            else if (_star1Shown)
                StopPulseExcept(star1);
        }

        private void HandleStar(Image star, ref bool shownFlag, bool active)
        {
            if (!star) return;

            if (active && !shownFlag)
            {
                shownFlag = true;
                star.enabled = true;
                PlayStarAnimation(star);
                
                if(star == star1) starFeel1?.PlayFeedbacks();
                else if(star == star2) starFeel2?.PlayFeedbacks();
                else if(star == star3) starFeel3?.PlayFeedbacks();
            }
            else if (!active)
            {
                star.enabled = false;
                shownFlag = false;
            }
        }

        private void PlayStarAnimation(Image star)
        {
            if (!star) return;

            star.transform.DOKill();
            star.color = Color.white;
            star.transform.localScale = Vector3.one * 0.8f;

            var s = DOTween.Sequence();
            s.Append(star.transform.DOScale(1.3f, 0.3f).SetEase(Ease.OutBack));
            s.Join(star.DOFade(1f, 0.3f));
            s.Append(star.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBounce));
            s.OnComplete(() =>
            {
                star.transform
                    .DOScale(1.05f, 0.6f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetId($"pulse_{star.GetInstanceID()}");
            });
        }

        private void StopPulseExcept(Image active)
        {
            StopPulse(star1, active);
            StopPulse(star2, active);
            StopPulse(star3, active);
        }

        private void StopPulse(Image star, Image active)
        {
            if (!star) return;
            if (star == active) return;

            DOTween.Kill($"pulse_{star.GetInstanceID()}");
            star.transform.DOKill();
            star.transform.localScale = Vector3.one;
        }
    }
}
