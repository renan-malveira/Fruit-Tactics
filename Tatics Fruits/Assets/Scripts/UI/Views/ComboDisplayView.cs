using Core.ScriptableObjects;
using Core.Services;
using DG.Tweening;
using Gameplay.Utils;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class ComboDisplayView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject    comboContainer;
        [SerializeField] private TextMeshProUGUI comboTierText;
        [SerializeField] private TextMeshProUGUI comboCountText;
        [SerializeField] private Slider        comboProgressBar;
        [SerializeField] private Image         progressFillImage;

        [Header("Screen Flash")]
        [SerializeField] private CanvasGroup screenFlashOverlay;
        [SerializeField] private float       flashInDuration  = 0.05f;
        [SerializeField] private float       flashOutDuration = 0.25f;

        [Header("VFX Root")]
        [SerializeField] private Transform vfxRoot;

        [Header("Tier Feedbacks")]
        [SerializeField] private MMF_Player tierUpFeedback;

        [Header("Bar Settings")]
        [SerializeField] private float fillAnimationDuration = 0.25f;
        [SerializeField] private Ease  fillEase              = Ease.OutCubic;

        private IComboTracker      _comboTracker;
        private ComboTierConfigSo  _tierConfig;
        private LevelConfigSo      _levelConfig;

        private Tween _barTween;
        private Tween _punchTween;
        private Tween _flashTween;

        private ComboTierConfigSo.ComboTier _lastTier;
        private int _lastTierIndex = -1;

        private static readonly string[] TierLocalizationKeys =
        {
            "combo",
            "combo_good",
            "combo_great",
            "combo_amazing",
            "combo_legendary",
        };

        public void Initialize(
            IComboTracker comboTracker,
            LevelConfigSo levelConfig,
            ComboTierConfigSo tierConfig)
        {
            _comboTracker = comboTracker;
            _tierConfig   = tierConfig;
            _levelConfig  = levelConfig;

            _comboTracker.OnComboChanged    += OnComboChanged;
            _comboTracker.OnComboTierChanged += OnComboTierChanged;

            comboContainer.SetActive(true);
            comboContainer.transform.localScale = Vector3.one;

            comboProgressBar.minValue    = 0f;
            comboProgressBar.maxValue    = 1f;
            comboProgressBar.value       = 0f;
            comboProgressBar.wholeNumbers = false;

            comboTierText.alpha  = 0f;
            comboCountText.alpha = 0f;

            if (screenFlashOverlay)
                screenFlashOverlay.alpha = 0f;

            if (Localizer.IsReady)
                Localizer.Instance.OnLanguageChanged += RefreshLocalization;
        }

        private void OnDestroy()
        {
            if (_comboTracker != null)
            {
                _comboTracker.OnComboChanged     -= OnComboChanged;
                _comboTracker.OnComboTierChanged -= OnComboTierChanged;
            }

            if (Localizer.IsReady)
                Localizer.Instance.OnLanguageChanged -= RefreshLocalization;

            _barTween?.Kill();
            _punchTween?.Kill();
            _flashTween?.Kill();

            DOTween.Kill(screenFlashOverlay);
        }

        private void OnComboChanged(int comboCount)
        {
            if (comboCount <= 0)
            {
                ResetVisuals();
                return;
            }

            UpdateCountText(comboCount);
            UpdateProgressBar(comboCount);
            PunchContainer(_lastTier);
        }

        private void OnComboTierChanged(int comboCount, ComboTierConfigSo.ComboTier tier)
        {
            if (comboCount <= 0 || tier == null)
                return;

            int newTierIndex = GetTierIndex(tier);
            bool isNewTier   = newTierIndex != _lastTierIndex;

            _lastTier      = tier;
            _lastTierIndex = newTierIndex;

            ApplyTierColors(tier);
            UpdateTierLabel(comboCount);

            if (isNewTier && comboCount > 1)
                PlayTierUpSequence(tier);
        }

        private void ApplyTierColors(ComboTierConfigSo.ComboTier tier)
        {
            comboTierText.color  = tier.tierColor;
            comboCountText.color = tier.tierColor;

            if (progressFillImage)
                progressFillImage.DOColor(tier.tierColor, 0.15f);
        }

        private void UpdateTierLabel(int comboCount)
        {
            var label = GetLocalizedTierName(comboCount);
            comboTierText.text = label;
            comboTierText.DOKill();
            comboTierText.DOFade(1f, 0.15f);
        }

        private void UpdateCountText(int comboCount)
        {
            if (comboCount <= 1)
            {
                comboCountText.DOFade(0f, 0.12f);
                comboCountText.text = string.Empty;
            }
            else
            {
                comboCountText.text = $"x{comboCount}";
                comboCountText.DOKill();
                comboCountText.DOFade(1f, 0.12f);
            }
        }

        private void UpdateProgressBar(int comboCount)
        {
            if (_tierConfig == null) return;

            var currentTier = _tierConfig.GetTierForCombo(comboCount);
            int tierMin     = currentTier.minComboCount;
            int tierMax     = _tierConfig.GetNextTierThreshold(comboCount);

            float fill = tierMin == tierMax
                ? 1f
                : Mathf.Clamp01((float)(comboCount - tierMin) / (tierMax - tierMin));

            _barTween?.Kill();
            _barTween = comboProgressBar
                .DOValue(fill, fillAnimationDuration)
                .SetEase(fillEase);
        }

        private void PlayTierUpSequence(ComboTierConfigSo.ComboTier tier)
        {
            tierUpFeedback?.PlayFeedbacks();

            if (tier.tierVFXPrefab && vfxRoot)
                Instantiate(tier.tierVFXPrefab, vfxRoot.position, Quaternion.identity, vfxRoot);

            PlayScreenFlash(tier);
        }

        private void PunchContainer(ComboTierConfigSo.ComboTier tier)
        {
            float scale    = tier?.punchScale    ?? 0.12f;
            float duration = tier?.punchDuration ?? 0.25f;

            _punchTween?.Kill();
            comboContainer.transform.localScale = Vector3.one;
            _punchTween = comboContainer.transform
                .DOPunchScale(Vector3.one * scale, duration, 1, 0.5f);
        }

        private void PlayScreenFlash(ComboTierConfigSo.ComboTier tier)
        {
            if (!screenFlashOverlay || tier.screenFlashAlpha <= 0f) return;

            var targetColor      = tier.screenFlashColor;
            targetColor.a        = tier.screenFlashAlpha;
            screenFlashOverlay.alpha = 0f;

            _flashTween?.Kill();
            _flashTween = DOTween.Sequence()
                .Append(screenFlashOverlay.DOFade(tier.screenFlashAlpha, flashInDuration).SetEase(Ease.OutQuad))
                .Append(screenFlashOverlay.DOFade(0f, flashOutDuration).SetEase(Ease.InQuad));
        }

        private void ResetVisuals()
        {
            _lastTier      = null;
            _lastTierIndex = -1;

            _barTween?.Kill();
            _barTween = comboProgressBar.DOValue(0f, 0.25f);

            comboTierText.DOKill();
            comboTierText.DOFade(0f, 0.2f);

            comboCountText.DOKill();
            comboCountText.DOFade(0f, 0.2f);
        }

        private int GetTierIndex(ComboTierConfigSo.ComboTier tier)
        {
            if (_tierConfig == null) return 0;
            for (int i = 0; i < _tierConfig.tiers.Length; i++)
                if (_tierConfig.tiers[i].minComboCount == tier.minComboCount)
                    return i;
            return 0;
        }

        private string GetLocalizedTierName(int comboCount)
        {
            if (_tierConfig == null) return string.Empty;

            int tierIndex = GetTierIndex(_tierConfig.GetTierForCombo(comboCount));
            var key       = tierIndex < TierLocalizationKeys.Length
                ? TierLocalizationKeys[tierIndex]
                : TierLocalizationKeys[TierLocalizationKeys.Length - 1];

            return Localizer.IsReady
                ? Localizer.Instance.Tr(key, key.ToUpper())
                : key.ToUpper();
        }

        private void RefreshLocalization()
        {
            if (_comboTracker != null && _comboTracker.CurrentCombo > 0)
                UpdateTierLabel(_comboTracker.CurrentCombo);
        }
    }
}
