using System;
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
            var levelId = _cfg.LevelKey;
            var total = _score.Total;
            var previousBest = _profileService.GetBestScore(levelId);

            // O recorde de pontos vale mesmo sem vencer; estrelas só com a meta batida.
            if (_profileService.RegisterBestScore(levelId, total))
                Debug.Log($"[DefeatPresenter] Novo recorde em {levelId}: {total}");
            var newRecord = total > previousBest;

            OnModelReady?.Invoke(new DefeatModel
            {
                levelId = levelId,
                totalScore = total,
                targetScore = _cfg.targetScore,
                starsEarned = 0,
                bestBefore = previousBest,
                newRecord = newRecord,
                timeLeftSeconds = _time.TimeLeftSeconds
            });

            // O anúncio de fim de partida agora sai na transição (GameControllerInitializer),
            // não aqui: antes a derrota contava duas vezes e o anúncio cobria a tela de resultado.
        }
    }
}
