using System;
using System.Threading;
using Core.SaveSystem;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class DeleteAccountView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        
        [Header("Dim")]
        [SerializeField] private CanvasGroup dimCanvasGroup;

        [Header("Initial Confirm")] 
        [SerializeField] private GameObject step1Root;
        [SerializeField] private Button step1YesButton;
        [SerializeField] private Button step1NoButton;
        
        [Header("Last Warning")] 
        [SerializeField] private GameObject step2Root;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Animação")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float dimTargetAlpha = 0.65f;
        [SerializeField] private float panelScaleFrom = 0.9f;
        [SerializeField] private float panelSlideOffsetY = -25f;

        [Header("Lógica")]
        [SerializeField] private float deleteDelaySeconds = 5f;

        private bool _isTransitioning;
        private Vector2 _panelOriginalPos;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            if (rootPanel == null)
                rootPanel = gameObject;

            if (panelRect != null)
                _panelOriginalPos = panelRect.anchoredPosition;

            PrepareInitialVisualState();
            HookButtons();
        }

        private void PrepareInitialVisualState()
        {
            if (dimCanvasGroup != null)
            {
                dimCanvasGroup.alpha = 0f;
                dimCanvasGroup.blocksRaycasts = true;
                dimCanvasGroup.interactable = false;
            }

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.blocksRaycasts = false;
                panelCanvasGroup.interactable = false;
            }

            if (panelRect != null)
            {
                panelRect.localScale = Vector3.one * panelScaleFrom;
                panelRect.anchoredPosition = _panelOriginalPos + new Vector2(0f, panelSlideOffsetY);
            }
            
            if(step1Root != null)
                step1Root.SetActive(true);
            
            if(step2Root != null)
                step2Root.SetActive(false);

            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(false);
                deleteButton.interactable = false;
            }

            if (countdownText != null)
            {
                countdownText.text = "";
            }
        }

        private void HookButtons()
        {
            if(step1YesButton != null)
                step1YesButton.onClick.AddListener(OnStep1Yes);
            
            if(step1NoButton != null)
                step1NoButton.onClick.AddListener(ClosePopup);
            
            if(cancelButton != null)
                cancelButton.onClick.AddListener(ClosePopup);
            
            if(deleteButton != null)
                deleteButton.onClick.AddListener(OnConfirmDelete);
        }

        public void Show()
        {
            rootPanel.SetActive(true);
            FadeIn();
        }

        private void FadeIn()
        {
            _isTransitioning = true;

            if (dimCanvasGroup != null)
            {
                dimCanvasGroup.DOFade(dimTargetAlpha, fadeDuration).SetEase(Ease.OutSine);
            }
            
            var seq = DOTween.Sequence();

            if (panelCanvasGroup != null)
            {
                seq.Join(panelCanvasGroup.DOFade(1f, fadeDuration));
            }

            if (panelRect != null)
            {
                seq.Join(panelRect.DOScale(1f, fadeDuration).SetEase(Ease.OutBack));
                seq.Join(panelRect.DOAnchorPos(_panelOriginalPos, fadeDuration).SetEase(Ease.OutSine));
            }

            seq.OnComplete(() =>
            {
                _isTransitioning = false;
                if (panelCanvasGroup != null)
                {
                    panelCanvasGroup.blocksRaycasts = true;
                    panelCanvasGroup.interactable = true;
                }
            });
        }

        private void FadeOutAndDestroy()
        {
            if(_isTransitioning)
                return;
            
            _isTransitioning = true;
        
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }

            var softDuration = fadeDuration * 1.2f;
            var softScale = 0.97f;
            var softMove = 8f;

            var seq = DOTween.Sequence();

            if (panelCanvasGroup != null)
            {
                seq.Join(panelCanvasGroup.DOFade(0f, softDuration).SetEase(Ease.OutSine));
            }

            if (panelRect != null)
            {
                seq.Join(panelRect.DOAnchorPos(_panelOriginalPos + new Vector2(0f, -softMove), softDuration)
                    .SetEase(Ease.OutSine));
            }
            
            seq.Join(panelRect.DOScale(softScale, softDuration).SetEase(Ease.OutSine));

            if (dimCanvasGroup != null)
            {
                seq.Join(dimCanvasGroup.DOFade(0f, softDuration * 0.9f).SetEase(Ease.OutSine));
            }
        
            seq.OnComplete(() =>
            {
                _isTransitioning = false;
                Destroy(gameObject);
            });
        }

        private void OnStep1Yes()
        {
            if(_isTransitioning)
                return;
            
            if(step1Root != null)
                step1Root.SetActive(false);
            
            if(step2Root != null)
                step2Root.SetActive(true);
            
            StartDeleteCountdown().Forget();
        }
        
        private async UniTaskVoid StartDeleteCountdown()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            var remaining = deleteDelaySeconds;

            while (remaining > 0f)
            {
                if (_cts.Token.IsCancellationRequested)
                    return;

                var secondsInt = Mathf.CeilToInt(remaining);
                if (countdownText != null)
                    countdownText.text = secondsInt.ToString();
                
                await UniTask.Delay(1000, cancellationToken: _cts.Token);
                remaining -= 1f;
            }
            
            if(countdownText != null)
                countdownText.text = "";

            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(true);
                deleteButton.interactable = true;
            }
        }

        private void OnConfirmDelete()
        {
            if (_isTransitioning)
                return;
            
            SaveManager.Instance.Delete<PlayerProfileData>();
            
            FadeOutAndDestroy();
        }

        private void ClosePopup()
        {
            FadeOutAndDestroy();
        }

        private void OnDestroy()
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
            
            if(panelRect != null)
                DOTween.Kill(panelRect);
            
            if(panelCanvasGroup != null)
                DOTween.Kill(panelCanvasGroup);
            
            if(dimCanvasGroup != null)
                DOTween.Kill(dimCanvasGroup);
        }
    }
}