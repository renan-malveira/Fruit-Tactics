using System;
using Ads;
using Core.ScriptableObjects;
using New_GameplayCore;
using UnityEngine;

namespace Core.Services
{
    [System.Serializable]
    public struct DefeatModel
    {
        public string levelId;
        public int totalScore;
        public int targetScore;
        public int starsEarned;
        public int bestBefore;
        public bool newRecord;
        public int timeLeftSeconds;
    }

    public class DefeatPresenter
    {
        private readonly LevelConfigSo _cfg;
        private readonly IScoreService _score;
        private readonly ITimeManager _time;
        private readonly PlayerProfileService _profileService;

        public event Action<DefeatModel> OnModelReady;
        public event Action OnReplay;
        public event Action OnMenu;
        
        public void ClickReplay() => OnReplay?.Invoke();
        public void ClickMenu() => OnMenu?.Invoke();

        public DefeatPresenter(LevelConfigSo cfg,
            IScoreService score,
            ITimeManager time,
            PlayerProfileService profileService)
        {
            _cfg = cfg;
            _score = score;
            _time = time;
            _profileService = profileService;
        }

        public void Build()
        {
            var levelId = string.IsNullOrEmpty(_cfg.levelId) ? _cfg.name : _cfg.levelId;
            var total = _score.Total;
            var previousBest = _profileService.GetBestScore(levelId);
            
            _profileService.RegisterBestScore(levelId, total);
            var newRecord = total > previousBest;

            var pct = Mathf.Clamp01(total / (float)_cfg.targetScore);
            var stars = 0;
            if (pct >= _cfg.star1Threshold) stars = 1;
            if (pct >= _cfg.star2Threshold) stars = 2;
            if (pct >= _cfg.star3Threshold) stars = 3;

            OnModelReady?.Invoke(new DefeatModel
            {
                levelId = levelId,
                totalScore = total,
                targetScore = _cfg.targetScore,
                starsEarned = stars,
                bestBefore = previousBest,
                newRecord = newRecord,
                timeLeftSeconds = _time.TimeLeftSeconds
            });
            
            InterstitialAdManager.Instance?.OnMatchCompleted();
        }
    }
}
