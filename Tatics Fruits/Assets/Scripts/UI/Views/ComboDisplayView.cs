using Core.ScriptableObjects;
using Core.Services;
using DG.Tweening;
using Gameplay.Utils;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class ComboDisplayView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject comboContainer;
        [SerializeField] private TextMeshProUGUI comboTierText;
        [SerializeField] private TextMeshProUGUI comboCountText;
        [SerializeField] private Slider comboProgressBar;

        [Header("Bar Settings")]
        [SerializeField] private float fillAnimationDuration = 0.3f;
        [SerializeField] private Ease fillEase = Ease.OutCubic;

        [Header("Animation Settings")]
        [SerializeField] private float punchScale = 1.2f;
        [SerializeField] private float punchDuration = 0.3f;

        private IComboTracker _comboTracker;
        private ComboTierConfigSo _tierConfig;

        private Tween _barTween;
        private Tween _punchTween;

        private static readonly string[] TierLocalizationKeys = new[]
        {
            "combo",
            "combo_good",
            "combo_great",
            "combo_amazing",
            "combo_legendary",
        };

        #region Initialization

        public void Initialize(
            IComboTracker comboTracker,
            LevelConfigSo levelConfig,
            ComboTierConfigSo tierConfig)
        {
            _comboTracker = comboTracker;
            _tierConfig = tierConfig;

            _comboTracker.OnComboChanged += OnComboChanged;
            
            comboContainer.SetActive(true);
            comboContainer.transform.localScale = Vector3.one;
            
            comboProgressBar.minValue = 0f;
            comboProgressBar.maxValue = 1f;
            comboProgressBar.value = 0f;
            comboProgressBar.wholeNumbers = false;

            comboTierText.alpha = 0f;
            comboCountText.alpha = 0f;

            if (Localizer.IsReady)
                Localizer.Instance.OnLanguageChanged += RefreshLocalization;
        }

        private void OnDestroy()
        {
            if (_comboTracker != null)
                _comboTracker.OnComboChanged -= OnComboChanged;

            if (Localizer.IsReady)
                Localizer.Instance.OnLanguageChanged -= RefreshLocalization;

            _barTween?.Kill();
            _punchTween?.Kill();
        }

        #endregion

        #region Combo Logic

        private void OnComboChanged(int comboCount)
        {
            if (comboCount <= 0)
            {
                ResetVisuals();
                return;
            }

            comboCount = Mathf.Clamp(comboCount, 1, 5);

            ApplyStage(comboCount);
            Punch();
        }

        private void ApplyStage(int stage)
        {
            var tier = _tierConfig.GetTierForCombo(stage);

            var localizedTierName = GetLocalizedTierName(stage);
            
            comboTierText.text = localizedTierName;
            comboTierText.color = tier.tierColor;
            comboTierText.DOKill();
            comboTierText.DOFade(1f, 0.2f);
            
            if (stage == 1)
            {
                comboCountText.text = "";
            }
            else
            {
                comboCountText.text = $"x{stage}";
                comboCountText.DOFade(1f, 0.2f);
            }
            
            var targetFill = stage == 1 ? 0.5f : 1f;

            _barTween?.Kill();
            _barTween = comboProgressBar
                .DOValue(targetFill, fillAnimationDuration)
                .SetEase(fillEase);
            
            if (comboProgressBar.fillRect != null)
            {
                var fillImage = comboProgressBar.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.DOColor(tier.tierColor, 0.15f);
                }
            }
        }

        private string GetLocalizedTierName(int stage)
        {
            var index = Mathf.Clamp(stage - 1, 0, TierLocalizationKeys.Length - 1);
            var key = TierLocalizationKeys[index];

            if (Localizer.IsReady)
                return Localizer.Instance.Tr(key, key.ToUpper());
            
            return key.ToUpper();
        }

        private void RefreshLocalization()
        {
            if(_comboTracker != null && _comboTracker.CurrentCombo > 0)
            {
                var tier = _tierConfig.GetTierForCombo(_comboTracker.CurrentCombo);
                comboTierText.text = GetLocalizedTierName(_comboTracker.CurrentCombo);
            }
        }

        private void ResetVisuals()
        {
            _barTween?.Kill();
            comboProgressBar.DOValue(0f, 0.25f);

            comboTierText.DOKill();
            comboTierText.DOFade(0f, 0.2f);

            comboCountText.DOKill();
            comboCountText.DOFade(0f, 0.2f);
        }

        private void Punch()
        {
            _punchTween?.Kill();
            comboContainer.transform.localScale = Vector3.one;

            _punchTween = comboContainer.transform
                .DOPunchScale(Vector3.one * punchScale, punchDuration, 1, 0.5f);
        }

        #endregion
    }
}
