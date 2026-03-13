using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class DailyMissionsPanelTabs : MonoBehaviour
    {
        [Header("Logic")]
        [SerializeField] private DailyMissionsController controller;
        [SerializeField] private PlayerProfileController profile;

        [Header("Root / Anim")]
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private RectTransform window;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimButton;

        [Header("Tabs (buttons)")]
        [SerializeField] private Button missionsTabButton;
        [SerializeField] private Button bonusTabButton;
        [SerializeField] private Image missionsTabBg;
        [SerializeField] private Image bonusTabBg;
        [SerializeField] private Color tabSelected = Color.white;
        [SerializeField] private Color tabUnselected = new Color(1,1,1,0.5f);
        [SerializeField] private GameObject bonusTabBadge;
        [SerializeField] private GameObject missionsTabBadge;

        [Header("Panels (contents)")]
        [SerializeField] private GameObject missionsPanelRoot;
        [SerializeField] private CanvasGroup missionsPanelCg;
        [SerializeField] private GameObject bonusPanelRoot;
        [SerializeField] private CanvasGroup bonusPanelCg;

        [Header("Missions List")]
        [SerializeField] private Transform missionsParent;
        [SerializeField] private DailyMissionItemView missionItemPrefab;

        [Header("Daily Login (grid 7/7)")]
        [SerializeField] private Transform loginGridParent;
        [SerializeField] private Transform loginSpecialGridParent;
        [SerializeField] private DailyLoginDayItemView loginDayPrefab;
        [SerializeField] private DailyLoginDayItemView loginDaySpecialPrefab;

        [Header("Defaults")]
        [SerializeField] private bool startOnMissions = true;

        [Header("Daily Reset")]
        [SerializeField] private TextMeshProUGUI resetCountdownText;
        [SerializeField] private float countdownRefreshSeconds = 0.5f;

        private bool _missionsBuilt;
        private bool _loginBuilt;
        private readonly List<DailyLoginDayItemView> _loginItems = new();

        private enum Tab { Missions, Bonus }
        private Tab _current;
        private Coroutine _countdownCo;
    
        private bool _animating;

        private void Awake()
        {
            if (closeButton) closeButton.onClick.AddListener(Hide);
            if (dimButton)   dimButton.onClick.AddListener(Hide);

            missionsTabButton.onClick.AddListener(() => SwitchTo(Tab.Missions));
            bonusTabButton.onClick.AddListener(() => SwitchTo(Tab.Bonus));
        }

        private void OnEnable()
        {
            controller.EnsureDayGenerated();
            controller.OnDailyLoginChanged += RefreshLoginGrid;
            controller.OnDailyMissionsChanged += HandleMissionsChanged;

            if (profile) profile.RequestShowGoldHud();

            if (startOnMissions) SwitchTo(Tab.Missions, instant:true);
            else                 SwitchTo(Tab.Bonus,    instant:true);

            UpdateMissionsTabBadge();
            UpdateBonusTabBadge();
            StartCountdown();

            Show();
        }

        private void OnDisable()
        {
            controller.OnDailyLoginChanged -= RefreshLoginGrid;
            controller.OnDailyMissionsChanged -= HandleMissionsChanged;

            if (profile) profile.ReleaseShowGoldHud();

            if (_countdownCo != null)
            {
                StopCoroutine(_countdownCo);
                _countdownCo = null;
            }

            DOTween.Kill(panelCanvasGroup);
            DOTween.Kill(window);
            if (missionsPanelCg) DOTween.Kill(missionsPanelCg);
            if (bonusPanelCg)    DOTween.Kill(bonusPanelCg);
        
            if (panelCanvasGroup)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }
            _animating = false;
        }

        private void UpdateBonusTabBadge()
        {
            if (!bonusTabBadge || controller == null) return;
            var hasClaim = controller.IsDailyLoginAvailable();
            bonusTabBadge.SetActive(hasClaim);
        }
    
        private void UpdateMissionsTabBadge()
        {
            if (!missionsTabBadge || controller == null) return;
            missionsTabBadge.SetActive(controller.HasMissionClaimAvailable());
        }

        private void HandleMissionsChanged()
        {
            UpdateMissionsTabBadge();
            if (_current == Tab.Missions)
                RefreshAllMissionItems();
        }
    

        public void Show()
        {
            if (_animating) return;
            _animating = true;

            gameObject.SetActive(true);
        
            Managers.AnalyticsManager.Instance?.TrackMenuOpened("daily_missions");

            if (panelCanvasGroup)
            {
                panelCanvasGroup.DOKill();
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }
            if (window)
            {
                window.DOKill();
                window.localScale = Vector3.one * 0.9f;
            }

            DOTween.Sequence()
                .Append(panelCanvasGroup ? panelCanvasGroup.DOFade(1f, 0.18f) : null)
                .Join(window ? window.DOScale(1f, 0.22f).SetEase(Ease.OutBack) : null)
                .OnComplete(() =>
                {
                    if (panelCanvasGroup)
                    {
                        panelCanvasGroup.interactable = true;
                        panelCanvasGroup.blocksRaycasts = true;
                    }
                    _animating = false;
                });
        }

        public void Hide()
        {
            if (_animating) return;
            _animating = true;

            if (panelCanvasGroup)
            {
                panelCanvasGroup.DOKill();
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }
            if (window) window.DOKill();

            DOTween.Sequence()
                .Append(panelCanvasGroup ? panelCanvasGroup.DOFade(0f, 0.15f) : null)
                .Join(window ? window.DOScale(0.94f, 0.15f).SetEase(Ease.InSine) : null)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    _animating = false;
                });
        }

        private void SwitchTo(Tab tab, bool instant = false)
        {
            _current = tab;

            missionsPanelRoot.SetActive(tab == Tab.Missions);
            bonusPanelRoot.SetActive(tab == Tab.Bonus);
        
            if (!instant)
            {
                if (missionsPanelCg) missionsPanelCg.alpha = (tab == Tab.Missions) ? 0f : 1f;
                if (bonusPanelCg)    bonusPanelCg.alpha    = (tab == Tab.Bonus)    ? 0f : 1f;

                if (tab == Tab.Missions && missionsPanelCg)
                    missionsPanelCg.DOFade(1f, 0.18f);
                else if (tab == Tab.Bonus && bonusPanelCg)
                    bonusPanelCg.DOFade(1f, 0.18f);
            }
            else
            {
                if (missionsPanelCg) missionsPanelCg.alpha = (tab == Tab.Missions) ? 1f : 0f;
                if (bonusPanelCg)    bonusPanelCg.alpha    = (tab == Tab.Bonus)    ? 1f : 0f;
            }
        
            if (missionsTabBg) missionsTabBg.color = (tab == Tab.Missions) ? tabSelected : tabUnselected;
            if (bonusTabBg)    bonusTabBg.color    = (tab == Tab.Bonus)    ? tabSelected : tabUnselected;
        
            if (tab == Tab.Missions)
            {
                BuildMissionsIfNeeded();
                RefreshAllMissionItems();
            }
            else
            {
                BuildLoginGridIfNeeded();
                RefreshLoginGrid();
            }
        
            UpdateMissionsTabBadge();
            UpdateBonusTabBadge();
        }
    
        private void BuildMissionsIfNeeded()
        {
            if (_missionsBuilt || missionItemPrefab == null || missionsParent == null) return;

            foreach (Transform t in missionsParent) Destroy(t.gameObject);

            var list = controller.GetMissions();
            foreach (var st in list)
            {
                var def = controller.GetDefinition(st.missionId);
                var item = Instantiate(missionItemPrefab, missionsParent);
                item.Setup(controller, st, def);
            }
            _missionsBuilt = true;
        }

        private void RefreshAllMissionItems()
        {
            foreach (Transform t in missionsParent)
            {
                var item = t.GetComponent<DailyMissionItemView>();
                if (item) item.Refresh();
            }
            UpdateMissionsTabBadge();
        }

        private void StartCountdown()
        {
            if (_countdownCo != null) StopCoroutine(_countdownCo);
            _countdownCo = StartCoroutine(CountdownLoop());
        }

        private IEnumerator CountdownLoop()
        {
            while (isActiveAndEnabled)
            {
                var now = controller.GetNow();
                var next = controller.GetNextResetTime();
                var remaining = next - now;

                if (remaining <= TimeSpan.Zero)
                {
                    controller.EnsureDayGenerated();

                    if (_current == Tab.Missions)
                    {
                        _missionsBuilt = false;
                        BuildMissionsIfNeeded();
                        RefreshAllMissionItems();
                    }
                    else
                    {
                        RefreshLoginGrid();
                        UpdateBonusTabBadge();
                        UpdateMissionsTabBadge();
                    }

                    controller.FireAttention();
                
                    now = controller.GetNow();
                    next = controller.GetNextResetTime();
                    remaining = next - now;
                }

                if (resetCountdownText)
                    resetCountdownText.text = FormatRemaining(remaining);

                yield return new WaitForSeconds(Mathf.Max(0.1f, countdownRefreshSeconds));
            }
        }

        private string FormatRemaining(TimeSpan t)
        {
            var hours = (int)Math.Floor(t.TotalHours);
            var minutes = t.Minutes;
            var seconds = t.Seconds;
            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }

        private void BuildLoginGridIfNeeded()
        {
            if (_loginBuilt || loginGridParent == null || loginDayPrefab == null) return;
        
            foreach (Transform t in loginGridParent) Destroy(t.gameObject);
            if (loginSpecialGridParent) foreach (Transform t in loginSpecialGridParent) Destroy(t.gameObject);

            _loginItems.Clear();

            var days = controller.GetLoginDays();
            for (int i = 0; i < days.Count; i++)
            {
                var info = days[i];
                bool isSpecial   = (i == 6) && (loginDaySpecialPrefab != null);
                var prefab       = isSpecial ? loginDaySpecialPrefab : loginDayPrefab;
                var targetParent = (isSpecial && loginSpecialGridParent != null) ? loginSpecialGridParent : loginGridParent;

                var item = Instantiate(prefab, targetParent);
                item.Setup(controller, info);
                _loginItems.Add(item);
            }

            _loginBuilt = true;
        }

        private void RefreshLoginGrid()
        {
            if (!_loginBuilt) return;

            var days = controller.GetLoginDays();
            for (int i = 0; i < _loginItems.Count && i < days.Count; i++)
            {
                _loginItems[i].Refresh(days[i]);
            }
            UpdateBonusTabBadge();
        }
    }
}
