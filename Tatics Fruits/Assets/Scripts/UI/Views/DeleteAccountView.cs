using System;
using System.Threading;
using Core.SaveSystem;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private bool _isDeleting;
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
                dimCanvasGroup.DOFade(dimTargetAlpha, fadeDuration).SetEase(Ease.OutSine).SetLink(gameObject);
            }

            // SetLink: a animação morre junto com o popup (antes seguia rodando após o Destroy).
            var seq = DOTween.Sequence().SetLink(gameObject);

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

            var seq = DOTween.Sequence().SetLink(gameObject);

            if (panelCanvasGroup != null)
            {
                seq.Join(panelCanvasGroup.DOFade(0f, softDuration).SetEase(Ease.OutSine));
            }

            if (panelRect != null)
            {
                seq.Join(panelRect.DOAnchorPos(_panelOriginalPos + new Vector2(0f, -softMove), softDuration)
                    .SetEase(Ease.OutSine));
                seq.Join(panelRect.DOScale(softScale, softDuration).SetEase(Ease.OutSine));
            }

            if (dimCanvasGroup != null)
            {
                seq.Join(dimCanvasGroup.DOFade(0f, softDuration * 0.9f).SetEase(Ease.OutSine));
            }
        
            seq.OnComplete(() =>
            {
                _isTransitioning = false;
                if (this != null)
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
            var token = _cts.Token; // o campo vira null quando o popup fecha

            var remaining = deleteDelaySeconds;

            while (remaining > 0f)
            {
                if (token.IsCancellationRequested || this == null)
                    return;

                var secondsInt = Mathf.CeilToInt(remaining);
                if (countdownText != null)
                    countdownText.text = secondsInt.ToString();

                var cancelled = await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: token)
                    .SuppressCancellationThrow();
                if (cancelled || this == null)
                    return;
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
            if (_isTransitioning || _isDeleting)
                return;

            RunDeletion().Forget();
        }

        private async UniTaskVoid RunDeletion()
        {
            _isDeleting = true;
            SetButtonsInteractable(false);
            SetStatus("delete_account_in_progress", "Excluindo sua conta...");

            var result = await Core.Privacy.AccountDeletionService.DeleteAccountAsync();

            if (this == null) return;

            switch (result)
            {
                case Core.Privacy.AccountDeletionService.Result.Deleted:
                case Core.Privacy.AccountDeletionService.Result.DataDeletedLoginSignedOut:
                    SetStatus("delete_account_done", "Conta excluída.");
                    await UniTask.Delay(1500, ignoreTimeScale: true);
                    // Recomeça do zero: termos, consentimento e um perfil novo.
                    SceneManager.LoadScene(0);
                    return;

                case Core.Privacy.AccountDeletionService.Result.Offline:
                    SetStatus("delete_account_offline", "Sem internet. Conecte-se e tente de novo.");
                    break;

                default:
                    SetStatus("delete_account_failed", "Não foi possível excluir agora. Tente de novo em instantes.");
                    break;
            }

            _isDeleting = false;
            SetButtonsInteractable(true);
        }

        private void SetButtonsInteractable(bool value)
        {
            if (deleteButton != null) deleteButton.interactable = value;
            if (cancelButton != null) cancelButton.interactable = value;
        }

        private void SetStatus(string key, string fallback)
        {
            if (countdownText == null) return;
            var loc = Gameplay.Utils.Localizer.Instance;
            countdownText.text = loc != null ? loc.Tr(key, fallback) : fallback;
        }

        private void ClosePopup()
        {
            FadeOutAndDestroy();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
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