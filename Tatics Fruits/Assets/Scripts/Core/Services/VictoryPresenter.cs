using System;
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
        private readonly int _mistakes;

        public Action<VictoryModel> OnModelReady;
        public event Action OnNext;
        public event Action OnReplay;
        public event Action OnMenu;

        public VictoryPresenter(
            LevelConfigSo cfg,
            ScoreService score,
            ITimeManager time,
            PlayerProfileService profileService,
            ILevelProgressService progress,
            LevelSetSO levelSet,
            int mistakes = 0)
        {
            _cfg = cfg;
            _score = score;
            _time = time;
            _profileService = profileService;
            _progress = progress;
            _levelSet = levelSet;
            _mistakes = Mathf.Max(0, mistakes);
        }

        public void Build()
        {
            var total = _score.Total;
            var target = _cfg.targetScore;
            var levelId = _cfg.LevelKey;
            var levelIndex = _progress.CurrentIndex;

            var stars = StarRules.Evaluate(_cfg, total);

            // Lê o recorde e as estrelas ANTES de gravar o resultado novo.
            var previousBest = _profileService.GetBestScore(levelId);
            var previousStars = _profileService.GetLevelStars(levelId);
            var newRecord = total > previousBest;

            _progress.RecordResult(_cfg, total, stars);

            OnModelReady?.Invoke(new VictoryModel
            {
                levelId = levelId,
                totalScore = total,
                targetScore = target,
                bestBefore = previousBest,
                newRecord = newRecord,
                starsEarned = stars,
                previousStars = previousStars,
                canGoNext = true,
                levelIndex = levelIndex,
                timeLeftSeconds = _time.TimeLeftSeconds,
                elapsedSeconds = Mathf.CeilToInt(_time.ElapsedSeconds),
                parTimeSeconds = _cfg.parTimeSeconds,
                mistakes = _mistakes
            });
        }

        public void ClickNext() => OnNext?.Invoke();
        public void ClickReplay() => OnReplay?.Invoke();
        public void ClickMenu() => OnMenu?.Invoke();
    }
}
