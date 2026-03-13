using System;
using Core.ScriptableObjects;
using Core.Services;
using UnityEngine;

namespace New_GameplayCore.Services
{
    public class LevelProgressService : ILevelProgressService
    {
        private const string FILE = "levels_progress.json";
        private LevelProgressData _data = new();

        public int CurrentIndex => _data.currentIndex;
        public int UnlockedMaxIndex => _data.unlockedMaxIndex;

        public void Load()
        {
            if(!JsonDataService.TryLoad(FILE, out _data))
                _data = new LevelProgressData();
        }

        public void Save()
        {
            JsonDataService.Save(FILE, _data);
        }

        public void SetCurrentIndex(int idx)
        {
            _data.currentIndex = Mathf.Max(0, idx);
            Save();
        }

        public LevelConfigSO Current(LevelSetSO set)
        {
            if(set == null || set.levels == null || set.levels.Length == 0)
                return null;
            
            _data.currentIndex = Mathf.Clamp(_data.currentIndex, 0, set.levels.Length -1);
            return set.levels[_data.currentIndex];
        }

        public void RecordResult(LevelConfigSO cfg, int totalScore, int stars)
        {
            var id = string.IsNullOrEmpty(cfg.levelId) ? cfg.name : cfg.levelId;
            if(!_data.best.TryGetValue(id, out var prev) || totalScore > prev)
                _data.best[id] = totalScore;
            
            _data.stars[id] = Mathf.Max(_data.stars.ContainsKey(id) ?  _data.stars[id] : 0, Mathf.Clamp(stars, 0,3));
        }

        public bool CanAdvance(LevelConfigSO cfg, int totalScore, float unlockPct = 0.75f)
        {
            if (cfg.targetScore <= 0)
                return false;
            
            var pct = totalScore / (float) cfg.targetScore;
            return pct >= unlockPct;
        }

        public void Advance(LevelSetSO set)
        {
            if(set == null || set.levels == null)
                return;
            
            _data.currentIndex = Mathf.Min(_data.currentIndex + 1, set.levels.Length -1);
            _data.unlockedMaxIndex = Mathf.Max(_data.unlockedMaxIndex, _data.currentIndex);
            Save();
        }

        public void Replay() => Save();

        public void MarkNextUnlockedIfEligible(LevelConfigSO cfg, int totalScore, float unlockPct = 0.75f)
        {
            if(cfg == null || cfg.targetScore <= 0)
                return;
            
            var pct = totalScore / (float) cfg.targetScore;
            if (pct >= unlockPct)
            {
                _data.unlockedMaxIndex = Math.Max(_data.unlockedMaxIndex, _data.currentIndex +1);
                Save();
            }
        }
        
        public void Reset()
        {
            _data = new LevelProgressData();
            Save();
        }

        public int TotalStars(LevelSetSO set)
        {
            if (set?.levels == null) return 0;

            var total = 0;
            foreach (var lvl in set.levels)
            {
                var id = string.IsNullOrEmpty(lvl.levelId) ? lvl.name : lvl.levelId;
                if (_data.stars.TryGetValue(id, out var s))
                    total += s;
            }
            return total;
        }

    }
}