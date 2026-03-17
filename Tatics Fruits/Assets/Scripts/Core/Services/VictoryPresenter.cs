using System;
using Ads;
using Core.ScriptableObjects;
using New_GameplayCore;
using New_GameplayCore.Services;
using UnityEngine;

namespace Core.Services
{
    public class VictoryPresenter
    {
        private readonly LevelConfigSo _cfg;
        private readonly IScoreService _score;
        private readonly ITimeManager  _time;
        private readonly PlayerProfileService _profileService;
        private readonly ILevelProgressService _progress;
        private readonly LevelSetSO _levelSet;

        public Action<VictoryModel> OnModelReady;
        public event Action OnNext;
        public event Action OnReplay;

        public VictoryPresenter(
            LevelConfigSo cfg,
            ScoreService score,
            ITimeManager time,
            PlayerProfileService profileService,
            ILevelProgressService progress,
            LevelSetSO levelSet)
        {
            _cfg = cfg;
            _score = score;
            _time = time;
            _profileService = profileService;
            _progress = progress;
            _levelSet = levelSet;
        }

        public void Build()
        {
            var total = _score.Total;
            var target = _cfg.targetScore;
            
            var pct = Mathf.Clamp01(total/(float)target);
            var stars = 0;
            if(pct >= _cfg.star1Threshold) stars = 1;
            if(pct >= _cfg.star2Threshold) stars = 2;
            if(pct >= _cfg.star3Threshold || total >= target) stars = 3;

            var levelId = string.IsNullOrEmpty(_cfg.levelId) ? _cfg.name : _cfg.levelId;
            var previousBest = _profileService.GetBestScore(levelId);
            
            _profileService.RegisterBestScore(levelId, total);
            var newRecord = total > previousBest;
            
            _progress.RecordResult(_cfg, total, stars);

            var canNext = _progress.CanAdvance(_cfg, total, 0.75f);

            OnModelReady?.Invoke(new VictoryModel
            {
                totalScore = total,
                targetScore = target,
                bestBefore = previousBest,
                newRecord = newRecord,
                starsEarned = stars,
                canGoNext = canNext,
                levelIndex = _progress.CurrentIndex
            });
        }
        
        public void ClickNext() => OnNext?.Invoke();
        public void ClickReplay() => OnReplay?.Invoke();
    }
}
