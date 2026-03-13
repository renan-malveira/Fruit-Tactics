using System;
using System.Collections.Generic;
using DG.Tweening;
using New_GameplayCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class TutorialPanel : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button skipButton;

        [Header("Slide Content")]
        [SerializeField] private RectTransform slideContainer;
        [SerializeField] private Image slideImage;
        [SerializeField] private TextMeshProUGUI slideText;

        [Header("Progress Dots")]
        [SerializeField] private Transform dotsContainer;
        [SerializeField] private Image dotPrefab;
        [SerializeField] private Color dotActive   = Color.white;
        [SerializeField] private Color dotInactive = new Color(1f, 1f, 1f, 0.3f);

        [Header("Animation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration   = 0.25f;
        [SerializeField] private float slideDuration  = 0.22f;
        [SerializeField] private float slideOffsetX   = 900f;

        [Header("Slides")]
        [SerializeField] private List<TutorialSlideSo> slides = new();

        private int _currentIndex;
        private bool _isShowing;
        private bool _isAnimating;
        private readonly List<Image> _dots = new();

        public event Action OnTutorialFinished;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            nextButton?.onClick.AddListener(OnNextClicked);
            prevButton?.onClick.AddListener(OnPrevClicked);
            closeButton?.onClick.AddListener(FinishTutorial);
            skipButton?.onClick.AddListener(FinishTutorial);

            BuildDots();
            HideImmediate();
        }

        public void Show()
        {
            if (slides == null || slides.Count == 0)
            {
                FinishTutorial();
                return;
            }

            gameObject.SetActive(true);
            _isShowing     = true;
            _isAnimating   = false;
            _currentIndex  = 0;

            canvasGroup.alpha          = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable   = true;

            RefreshSlide();
            canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
        }

        public void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha          = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable   = false;
            }

            _isShowing = false;
            gameObject.SetActive(false);
        }

        private void OnNextClicked()
        {
            if (!_isShowing || _isAnimating) return;
            if (_currentIndex >= slides.Count - 1) return;

            TransitionTo(_currentIndex + 1, direction: 1);
        }

        private void OnPrevClicked()
        {
            if (!_isShowing || _isAnimating) return;
            if (_currentIndex <= 0) return;

            TransitionTo(_currentIndex - 1, direction: -1);
        }

        private void TransitionTo(int nextIndex, int direction)
        {
            if (slideContainer == null)
            {
                _currentIndex = nextIndex;
                RefreshSlide();
                return;
            }

            _isAnimating = true;

            slideContainer
                .DOLocalMoveX(-direction * slideOffsetX, slideDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    _currentIndex = nextIndex;
                    RefreshSlide();

                    slideContainer.localPosition = new Vector3(
                        direction * slideOffsetX,
                        slideContainer.localPosition.y,
                        0f);

                    slideContainer
                        .DOLocalMoveX(0f, slideDuration)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() => _isAnimating = false);
                });
        }

        private void RefreshSlide()
        {
            if (_currentIndex < 0 || _currentIndex >= slides.Count) return;

            var slide = slides[_currentIndex];

            if (slideImage != null)
                slideImage.sprite = slide.image;

            if (slideText != null)
            {
                var localized = slideText.GetComponent<LocalizedText>();
                if (localized != null && !string.IsNullOrEmpty(slide.localizationKey))
                {
                    localized.key      = slide.localizationKey;
                    localized.fallback = slide.description;
                    localized.Refresh();
                }
                else
                {
                    slideText.text = slide.description;
                }
            }

            RefreshButtons();
            RefreshDots();
        }

        private void RefreshButtons()
        {
            var isLast = _currentIndex == slides.Count - 1;

            prevButton?.gameObject.SetActive(_currentIndex > 0);
            nextButton?.gameObject.SetActive(!isLast);
            closeButton?.gameObject.SetActive(isLast);
        }

        private void RefreshDots()
        {
            for (int i = 0; i < _dots.Count; i++)
            {
                if (_dots[i] == null) continue;
                _dots[i].color = i == _currentIndex ? dotActive : dotInactive;

                _dots[i].transform
                    .DOScale(i == _currentIndex ? 1.3f : 1f, 0.15f)
                    .SetEase(Ease.OutBack);
            }
        }

        private void BuildDots()
        {
            if (dotsContainer == null || dotPrefab == null) return;

            foreach (Transform child in dotsContainer)
                Destroy(child.gameObject);

            _dots.Clear();

            foreach (var _ in slides)
            {
                var dot = Instantiate(dotPrefab, dotsContainer);
                dot.color = dotInactive;
                _dots.Add(dot);
            }
        }

        private void FinishTutorial()
        {
            if (!_isShowing) return;

            _isShowing = false;

            canvasGroup
                .DOFade(0f, fadeDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    HideImmediate();
                    OnTutorialFinished?.Invoke();
                });
        }

        private void OnDestroy()
        {
            DOTween.Kill(canvasGroup);
            if (slideContainer != null)
                DOTween.Kill(slideContainer);
        }
    }
}
