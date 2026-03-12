using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Managers;
using Services;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Controllers
{
    public class LeaderboardController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image overlay;

        [Header("List")]
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Transform content;
        [SerializeField] private LeaderboardEntryView rowPrefab;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private GameObject loadingState;

        [Header("Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button refreshButton;

        [Header("Data")]
        [SerializeField] private DataSaver dataSaver;

        private LeaderboardPresenter _presenter;
        private readonly List<LeaderboardEntryView> _rows = new();
        private string _currentPlayerId;

        private void Awake()
        {
            if (backButton)   backButton.onClick.AddListener(Close);
            if (refreshButton) refreshButton.onClick.AddListener(() => _presenter?.Fetch());

            if (canvasGroup)
            {
                canvasGroup.interactable   = true;
                canvasGroup.blocksRaycasts = true;
            }
            if (overlay) overlay.gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            var repository = new FirebaseLeaderboardRepository();
            _presenter = new LeaderboardPresenter(repository);
            _presenter.OnLoading   += OnLoading;
            _presenter.OnDataReady += OnDataReady;
            _presenter.OnError     += OnError;

            Open();
            
            StartCoroutine(FetchAfterFrame());
        }

        private IEnumerator FetchAfterFrame()
        {
            yield return null;

            var auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

            if (auth.CurrentUser != null)
            {
                _currentPlayerId = auth.CurrentUser.UserId;
                _presenter.Fetch();
            }
            else
            {
                if (loadingState) loadingState.SetActive(true);
                auth.StateChanged += OnAuthStateChanged;
            }
        }
        
        private void OnAuthStateChanged(object sender, EventArgs e)
        {
            var auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
            if (auth.CurrentUser == null) return;

            auth.StateChanged -= OnAuthStateChanged;
            _currentPlayerId = auth.CurrentUser.UserId;

            Services.MainThreadDispatcher.Enqueue(() => _presenter?.Fetch());
        }

        private void OnDisable()
        {
            Firebase.Auth.FirebaseAuth.DefaultInstance.StateChanged -= OnAuthStateChanged;

            if (_presenter == null) return;
            _presenter.OnLoading   -= OnLoading;
            _presenter.OnDataReady -= OnDataReady;
            _presenter.OnError     -= OnError;
        }

        private string GetFirebaseUserId()
        {
            return Firebase.Auth.FirebaseAuth.DefaultInstance?.CurrentUser?.UserId ?? string.Empty;
        }

        private void Open()
        {
            panel.anchoredPosition = new Vector2(60f, panel.anchoredPosition.y);
            canvasGroup.alpha = 0f;
            DOTween.Sequence()
                .Append(panel.DOAnchorPosX(0f, 0.25f).SetEase(Ease.OutQuad))
                .Join(canvasGroup.DOFade(1f, 0.25f));
        }

        public void Close()
        {
            DOTween.Sequence()
                .Append(panel.DOAnchorPosX(60f, 0.18f).SetEase(Ease.InQuad))
                .Join(canvasGroup.DOFade(0f, 0.18f))
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void OnLoading()
        {
            if (loadingState) loadingState.SetActive(true);
            if (emptyState)   emptyState.SetActive(false);
            ClearRows();
        }

        private void OnDataReady(List<LeaderboardEntry> entries)
        {
            if (loadingState) loadingState.SetActive(false);
            RenderList(entries);
        }

        private void OnError(System.Exception ex)
        {
            Debug.LogError($"[LeaderboardController] Failed to load: {ex}");
            if (loadingState) loadingState.SetActive(false);
            if (emptyState)   emptyState.SetActive(true);
        }

        private void RenderList(List<LeaderboardEntry> list)
        {
            ClearRows();

            var hasData = list != null && list.Count > 0;
            if (emptyState) emptyState.SetActive(!hasData);
            if (!hasData) return;

            var delay = 0f;
            foreach (var entry in list)
            {
                var row = Instantiate(rowPrefab, content);
                row.transform.localScale = Vector3.zero;

                var cg = row.GetComponent<CanvasGroup>();
                if (cg) cg.alpha = 0f;

                var isCurrent = !string.IsNullOrEmpty(_currentPlayerId) && entry.playerId == _currentPlayerId;
                row.Bind(entry, isCurrent);

                DOTween.Sequence()
                    .SetDelay(delay)
                    .Append(row.transform.DOScale(1f, 0.18f).SetEase(Ease.OutBack))
                    .Join(cg ? cg.DOFade(1f, 0.18f) : null);

                _rows.Add(row);
                delay += 0.05f;
            }
            
            if (content is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            scroll.verticalNormalizedPosition = 1f;
        }

        private void ClearRows()
        {
            foreach (var r in _rows) if (r) Destroy(r.gameObject);
            _rows.Clear();
        }
    }
}
