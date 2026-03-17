using System;
using System.Collections.Generic;
using System.Linq;
using Core.ScriptableObjects;
using Gameplay.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Controllers
{
    public class DailyMissionsController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PlayerProfileController profile;
        [SerializeField] private List<DailyMissionSo> missionPool;
        [SerializeField, Range(1, 5)] private int missionsPerDay = 3;
        [SerializeField] private bool useLocalTime = true;
        [SerializeField] private DailyLoginConfigSo loginConfig;

        public static DailyMissionsController Instance { get; private set; }
        
        private bool ProfileAlive => profile != null;

        public event Action<bool> OnAttentionChanged;
        public event Action OnDailyLoginChanged;
        public event Action OnDailyMissionsChanged;
        public bool UseLocalTime => useLocalTime;
        public DateTime GetNow() => useLocalTime ? DateTime.Now : DateTime.UtcNow;

        private string TodayKey =>
            (useLocalTime ? DateTime.Now : DateTime.UtcNow).ToString("yyyyMMdd");

        public struct DailyLoginDayInfo
        {
            public int Index;
            public int Reward;
            public bool Claimed;
            public bool Claimable;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            EnsureLoginInitialized();
            MigrateLegacyLoginIfNeeded();
            HandleStreakBreakIfNeeded();
            EnsureDayGenerated();
            FireAttention();
        }

        private int[] DefaultRewards()
        {
            if (loginConfig != null)
                return loginConfig.GetAllRewards();
            return new[] { 50, 75, 100, 150, 200, 300, 500 };
        }

        private void EnsureLoginInitialized()
        {
            if (!ProfileAlive) return;
            if (profile.Data == null) return;
            if (profile.Data.daily == null)
                profile.Data.daily = new DailySystemData();

            var l = profile.Data.daily.login;
            if (l == null)
            {
                profile.Data.daily.login = new DailyLoginData();
                l = profile.Data.daily.login;
            }

            var defaults = DefaultRewards();
            
            l.rewards = new List<int>(defaults);

            if (l.claimed == null || l.claimed.Count != defaults.Length)
                l.claimed = new List<bool>(new bool[defaults.Length]);

            l.cycleIndex = Mathf.Clamp(l.cycleIndex, 0, defaults.Length - 1);
        }

        private void MigrateLegacyLoginIfNeeded()
        {
            if (!ProfileAlive) return;
            var daily = profile.Data.daily;
            if (daily == null) return;

            var l = daily.login;
            if (l == null) { EnsureLoginInitialized(); l = daily.login; }

            if (!string.IsNullOrEmpty(daily.lastLoginRewardDayKey) &&
                string.IsNullOrEmpty(l.lastClaimDayKey))
                l.lastClaimDayKey = daily.lastLoginRewardDayKey;

            profile.SaveProfile();
        }

        private void HandleStreakBreakIfNeeded()
        {
            if (!ProfileAlive) return;
            if (loginConfig == null || !loginConfig.ResetStreakOnMiss) return;

            var l = profile.Data.daily.login;
            if (string.IsNullOrEmpty(l.lastClaimDayKey)) return;

            if (!DateTime.TryParseExact(l.lastClaimDayKey, "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var lastClaim)) return;

            var today = GetNow().Date;
            var daysSinceClaim = (today - lastClaim.Date).Days;

            if (daysSinceClaim <= 1) return;

            l.cycleIndex = 0;
            var defaults = DefaultRewards();
            l.claimed = new List<bool>(new bool[defaults.Length]);
            l.lastClaimDayKey = string.Empty;
            profile.SaveProfile();
        }

        public void EnsureDayGenerated()
        {
            if (!ProfileAlive) return;
            var daily = profile.Data.daily;
            if (daily == null)
            {
                profile.Data.daily = new DailySystemData();
                daily = profile.Data.daily;
            }

            if (daily.dayKey == TodayKey && daily.missions != null && daily.missions.Count == missionsPerDay)
                return;

            daily.dayKey = TodayKey;
            daily.missions = new List<DailyMissionState>();

            var pool = missionPool.Where(m => m != null).OrderBy(_ => UnityEngine.Random.value).ToList();
            for (int i = 0; i < Mathf.Min(missionsPerDay, pool.Count); i++)
            {
                var def = pool[i];
                var desc = def.descriptionTemplate;
                if (def.eventType == MissionEventType.WinLevel && def.levelParam > 0)
                    desc = desc.Replace("{level}", def.levelParam.ToString());

                daily.missions.Add(new DailyMissionState
                {
                    missionId = def.id,
                    description = desc,
                    progress = 0,
                    target = Mathf.Max(1, def.target),
                    rewardGold = Mathf.Max(0, def.rewardGold),
                    completed = false,
                    claimed = false
                });
            }

            profile.SaveProfile();
            OnDailyMissionsChanged?.Invoke();
            FireAttention();
        }

        public DailyMissionSo GetDefinition(string missionId)
            => missionPool.FirstOrDefault(m => m && m.id == missionId);

        public List<DailyLoginDayInfo> GetLoginDays()
        {
            if (!ProfileAlive) return new List<DailyLoginDayInfo>();
            EnsureLoginInitialized();
            var l = profile.Data.daily.login;
            var list = new List<DailyLoginDayInfo>(l.rewards.Count);
            for (int i = 0; i < l.rewards.Count; i++)
            {
                bool claimable = (i == l.cycleIndex) && l.lastClaimDayKey != TodayKey && !l.claimed[i];
                list.Add(new DailyLoginDayInfo
                {
                    Index = i,
                    Reward = loginConfig != null ? loginConfig.GetReward(i) : l.rewards[i],
                    Claimed = l.claimed[i],
                    Claimable = claimable
                });
            }
            return list;
        }

        public bool TryClaimDailyLoginDay(int index)
        {
            if (!ProfileAlive) return false;
            EnsureLoginInitialized();
            var l = profile.Data.daily.login;

            if (index != l.cycleIndex) return false;
            if (l.claimed[index]) return false;
            if (l.lastClaimDayKey == TodayKey) return false;

            var rewardCoins = loginConfig != null ? loginConfig.GetReward(index) : l.rewards[index];
            profile.AddGoldAndSave(rewardCoins);

            l.claimed[index] = true;
            l.lastClaimDayKey = TodayKey;

            l.cycleIndex++;
            if (l.cycleIndex >= l.rewards.Count)
            {
                l.cycleIndex = 0;
                for (int i = 0; i < l.claimed.Count; i++) l.claimed[i] = false;
            }

            profile.SaveProfile();
            SaveHelper.OnDailyRewardClaimed(TodayKey, rewardCoins);

            OnDailyLoginChanged?.Invoke();
            FireAttention();
            return true;
        }

        public bool IsDailyLoginAvailable()
        {
            if (!ProfileAlive) return false;
            EnsureLoginInitialized();
            var l = profile.Data.daily.login;
            return l.lastClaimDayKey != TodayKey && !l.claimed[l.cycleIndex];
        }

        public bool TryClaimDailyLogin()
        {
            if (!ProfileAlive) return false;
            EnsureLoginInitialized();
            var l = profile.Data.daily.login;
            return TryClaimDailyLoginDay(l.cycleIndex);
        }

        DailyMissionSo FindDef(string missionId) =>
            missionPool.FirstOrDefault(m => m && m.id == missionId);

        public IReadOnlyList<DailyMissionState> GetMissions()
        {
            if (!ProfileAlive) return Array.Empty<DailyMissionState>();
            return profile.Data?.daily?.missions ?? (IReadOnlyList<DailyMissionState>)Array.Empty<DailyMissionState>();
        }

        public bool TryClaimMission(string missionId)
        {
            if (!ProfileAlive) return false;
            var st = profile.Data.daily.missions.FirstOrDefault(m => m.missionId == missionId);
            if (st == null || !st.completed || st.claimed) return false;

            var rewardCoins = st.rewardGold;
            profile.AddGoldAndSave(rewardCoins);
            st.claimed = true;
            profile.SaveProfile();

            Managers.AnalyticsManager.Instance?.TrackDailyMissionCompleted(missionId, "daily_mission", rewardCoins);

            OnDailyMissionsChanged?.Invoke();
            FireAttention();
            return true;
        }

        public DateTime GetNextResetTime() => GetNow().Date.AddDays(1);

        public void ReportPairMade()
        {
            ReportSingleIncrement(MissionEventType.MakePairs);
        }

        public void ReportScore(int totalScore)
        {
            if (!ProfileAlive) return;
            var list = profile.Data.daily.missions;
            if (list == null)
                return;

            var changed = false;
            foreach (var st in list)
            {
                var def = FindDef(st.missionId);
                if (def == null ||  def.eventType != MissionEventType.ScorePoints) continue;
                if (st.completed) continue;
                
                st.progress = Mathf.Max(st.progress, totalScore);
                if (st.progress >= st.target && !st.completed)
                {
                    st.completed = true;
                    changed = true;
                }
                else if (st.progress > 0) changed = true;
            }

            if (changed) Flush();
        }
        
        public void ReportComboReached(int comboCount)
        {
            if (!ProfileAlive) return;
            var list = profile.Data.daily.missions;
            if (list == null) return;

            var changed = false;
            foreach (var st in list)
            {
                var def = FindDef(st.missionId);
                if (def == null || def.eventType != MissionEventType.ReachCombo) continue;
                if (st.completed) continue;
                if (comboCount < def.target) continue;

                st.progress = def.target;
                st.completed = true;
                changed = true;
            }

            if (changed) Flush();
        }

        public void ReportSessionStarted()
        {
            ReportSingleIncrement(MissionEventType.PlaySessions);
        }

        public void ReportWinWithoutMistakes(bool hadNoMistakes)
        {
            if (!hadNoMistakes) return;
            ReportSingleIncrement(MissionEventType.WinWithoutMistakes);
        }

        private void ReportSingleIncrement(MissionEventType type)
        {
            if (profile == null) return;

            var list = profile.Data?.daily?.missions;
            if (list == null) return;

            var changed = false;
            foreach (var st in list)
            {
                var def = FindDef(st.missionId);
                if (def == null || def.eventType != type) continue;
                if (st.completed) continue;

                st.progress = Mathf.Min(st.target, st.progress + 1);
                changed = true;
                if (st.progress >= st.target)
                    st.completed = true;
            }

            if (changed) Flush();
        }

        private void Flush()
        {
            if (!ProfileAlive) return;
            profile.SaveProfile();
            OnDailyMissionsChanged?.Invoke();
            FireAttention();
        }

        public void ReportWinLevel(int level)
        {
            if (!ProfileAlive) return;
            var list = profile.Data?.daily?.missions;
            if (list == null) return;

            var changed = false;
            foreach (var st in list)
            {
                var def = FindDef(st.missionId);
                if (def == null || def.eventType != MissionEventType.WinLevel) continue;
                if (def.levelParam > 0 && def.levelParam != level) continue;
                if (st.completed) continue;

                st.progress = Mathf.Min(st.target, st.progress + 1);
                changed = true;
                if (st.progress >= st.target)
                    st.completed = true;
            }

            if (changed) Flush();
        }

        public bool HasAnyClaimAvailable()
        {
            if (!ProfileAlive) return false;
            bool anyMission = profile.Data?.daily?.missions?.Any(m => m.completed && !m.claimed) ?? false;
            return anyMission || IsDailyLoginAvailable();
        }

        public bool HasMissionClaimAvailable()
        {
            if (!ProfileAlive) return false;
            var list = profile.Data?.daily?.missions;
            return list != null && list.Any(m => m.completed && !m.claimed);
        }

        public void FireAttention() => OnAttentionChanged?.Invoke(HasAnyClaimAvailable());
    }
}
