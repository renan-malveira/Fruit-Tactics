using Core.ScriptableObjects;
using Core.Services;
using DG.Tweening;
using MoreMountains.Feedbacks;
using New_GameplayCore.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class HUDView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Button swapAllButton;
        [SerializeField] private Button swapOneButton;
        [SerializeField] private RectTransform timeDeltaAnchor;
        [SerializeField] private TimeDeltaToast timeDeltaToast;

        [Header("Swap Cost Labels")]
        [SerializeField] private TextMeshProUGUI swapAllCostLabel;
        [SerializeField] private TextMeshProUGUI swapOneCostLabel;

        [Header("Penalty Flash")]
        [SerializeField] private CanvasGroup penaltyFlashOverlay;
        [SerializeField] private float flashPeakAlpha  = 0.35f;
        [SerializeField] private float flashInDuration = 0.06f;
        [SerializeField] private float flashOutDuration = 0.22f;

        [Header("Clock Shake")]
        [SerializeField] private GameObject clockObject;
        [SerializeField] private int shakeThreshold = 10;

        [Header("Shake Settings")]
        [SerializeField] private float idleShakeSpeed = 10f;
        [SerializeField] private float idleShakeRange = 0.05f;
        [SerializeField] private float dangerShakeSpeed = 30f;
        [SerializeField] private float dangerShakeRange = 0.2f;

        [Header("Score Feedback")]
        [SerializeField] private MMF_Player scoreFeedback;

        [Header("Swap Feedbacks")]
        [SerializeField] private MMF_Player swapAllFeedback;
        [SerializeField] private MMF_Player swapOneFeedback;
        [SerializeField] private MMF_Player swapFailFeedback;

        [Header("Swap Cooldown")]
        [SerializeField] private float swapCooldownSeconds = 0.5f;

        [Header("Combo Display")]
        [SerializeField] private ComboDisplayView comboDisplay;

        private ITimeManager _time;
        private IScoreService _score;
        private ISwapService _swap;
        private IComboTracker _comboTracker;
        private MMScaleShaker _clockShaker;
        private bool _isInDangerZone;

        public void Initialize(ITimeManager time, IScoreService score, ISwapService swap, IComboTracker comboTracker, LevelConfigSO levelConfig, ComboTierConfigSo tierConfig = null)
        {
            _time         = time;
            _score        = score;
            _swap         = swap;
            _comboTracker = comboTracker;

            if (clockObject != null)
            {
                _clockShaker = clockObject.GetComponent<MMScaleShaker>();
                if (_clockShaker != null)
                {
                    SetIdleShake();
                    _clockShaker.Play();
                }
            }

            if (comboDisplay != null)
                comboDisplay.Initialize(comboTracker, levelConfig, tierConfig);

            if (swapAllCostLabel != null)
                swapAllCostLabel.text = $"-{levelConfig.swapAllTimePenalty}s";

            if (swapOneCostLabel != null)
                swapOneCostLabel.text = $"-{levelConfig.swapRandomTimePenalty}s";

            if (penaltyFlashOverlay != null)
                penaltyFlashOverlay.alpha = 0f;

            _time.OnTimeChanged += UpdateTimer;
            _time.OnTimeDelta   += ShowTimeDelta;
            _score.OnScoreChanged += UpdateScore;

            _swap.OnSwapAllAttempted    += HandleSwapAllAttempted;
            _swap.OnSwapRandomAttempted += HandleSwapRandomAttempted;

            swapAllButton.onClick.AddListener(OnSwapAll);
            swapOneButton.onClick.AddListener(OnSwapOne);

            UpdateTimer(_time.TimeLeftSeconds);
            UpdateScore(_score.Total, 0);
        }

        private void OnDestroy()
        {
            if (_time != null)
            {
                _time.OnTimeChanged -= UpdateTimer;
                _time.OnTimeDelta   -= ShowTimeDelta;
            }

            if (_score != null)
                _score.OnScoreChanged -= UpdateScore;

            if (_swap != null)
            {
                _swap.OnSwapAllAttempted    -= HandleSwapAllAttempted;
                _swap.OnSwapRandomAttempted -= HandleSwapRandomAttempted;
            }

            if (_clockShaker != null)
                _clockShaker.Stop();

            DOTween.Kill(penaltyFlashOverlay);
        }

        private void OnSwapAll()
        {
            SetButtonsInteractable(false);
            _swap.TrySwapAll();
            DOVirtual.DelayedCall(swapCooldownSeconds, () => SetButtonsInteractable(true));
        }

        private void OnSwapOne()
        {
            SetButtonsInteractable(false);
            _swap.TrySwapRandom();
            DOVirtual.DelayedCall(swapCooldownSeconds, () => SetButtonsInteractable(true));
        }

        private void HandleSwapAllAttempted(bool success, int penalty)
        {
            if (success)
                swapAllFeedback?.PlayFeedbacks();
            else
                PlayFailPunch(swapAllButton.transform as RectTransform);
        }

        private void HandleSwapRandomAttempted(bool success, int penalty)
        {
            if (success)
                swapOneFeedback?.PlayFeedbacks();
            else
                PlayFailPunch(swapOneButton.transform as RectTransform);
        }

        private void PlayFailPunch(RectTransform target)
        {
            swapFailFeedback?.PlayFeedbacks();

            if (target != null)
            {
                DOTween.Kill(target);
                target.DOShakeAnchorPos(0.3f, strength: 12f, vibrato: 20, randomness: 0f, snapping: false, fadeOut: true);
            }
        }

        private void PlayPenaltyFlash()
        {
            if (penaltyFlashOverlay == null) return;

            DOTween.Kill(penaltyFlashOverlay);
            penaltyFlashOverlay.alpha = 0f;
            penaltyFlashOverlay
                .DOFade(flashPeakAlpha, flashInDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                    penaltyFlashOverlay
                        .DOFade(0f, flashOutDuration)
                        .SetEase(Ease.InQuad));
        }

        private void ShowTimeDelta(int delta)
        {
            if (!timeDeltaToast || !timeDeltaAnchor) return;

            if (delta < 0) PlayPenaltyFlash();

            var toast = Instantiate(timeDeltaToast, timeDeltaAnchor);
            toast.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            toast.Play(delta);
        }

        private void UpdateTimer(int value)
        {
            timerText.text = value.ToString("00");

            if (value <= shakeThreshold && value > 0)
            {
                timerText.color = Color.red;
                if (!_isInDangerZone && _clockShaker != null)
                {
                    SetDangerShake();
                    _isInDangerZone = true;
                }
            }
            else
            {
                timerText.color = Color.white;
                if (_isInDangerZone && _clockShaker != null)
                {
                    SetIdleShake();
                    _isInDangerZone = false;
                }
            }
        }

        private void UpdateScore(int total, int delta)
        {
            scoreText.text = total.ToString("N0");
            if (delta > 0) scoreFeedback?.PlayFeedbacks();
        }

        private void SetButtonsInteractable(bool value)
        {
            swapAllButton.interactable = value;
            swapOneButton.interactable = value;
        }

        private void SetIdleShake()
        {
            _clockShaker.ShakeSpeed = idleShakeSpeed;
            _clockShaker.ShakeRange = idleShakeRange;
        }

        private void SetDangerShake()
        {
            _clockShaker.ShakeSpeed = dangerShakeSpeed;
            _clockShaker.ShakeRange = dangerShakeRange;
        }
    }
}
