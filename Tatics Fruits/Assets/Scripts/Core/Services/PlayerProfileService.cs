using System.Collections.Generic;
using Core.SaveSystem;
using Gameplay.Utils;

namespace Core.Services
{
    public class PlayerProfileService
    {
        private PlayerProfileData _data;

        public PlayerProfileData Data => _data;

        public void Initialize(PlayerProfileData data)
        {
            _data = data;
        }

        public void Load()
        {
            _data = SaveManager.Instance.Load<PlayerProfileData>();
        }
        
        public void Save()
        {
            SaveManager.Instance.Save(_data);
        }

        public void MarkDirty()
        {
            SaveManager.Instance.MarkDirty<PlayerProfileData>();
        }

        public void AddGold(int amount)
        {
            _data.gold += amount;
            if (_data.gold < 0)
                _data.gold = 0;
            Save();
        }

        public void SetLevel(int index, bool completed = false)
        {
            _data.currentLevelIndex = index;
            if (completed && index >= _data.highestLevelUnlocked)
                _data.highestLevelUnlocked = index + 1;
            Save();
        }

        public void SetSettings(bool music, bool sfx, bool vfx, string lang)
        {
            _data.musicOn = music;
            _data.sfxOn = sfx;
            _data.vfxOn = vfx;
            _data.language = lang;
            Save();
        }

        public void UpdateDailyLogin(string todayKey)
        {
            if (_data.daily.login.lastClaimDayKey != todayKey)
            {
                _data.daily.login.lastClaimDayKey = todayKey;
                Save();
            }
        }

        public void UpdateDailyMissions(List<DailyMissionState> missions)
        {
            _data.daily.missions = missions;
            Save();
        }

        public bool RegisterBestScore(string levelId, int score)
        {
            if (string.IsNullOrEmpty(levelId))
                return false;

            if (!_data.BestScores.ContainsKey(levelId))
            {
                _data.BestScores[levelId] = score;
                Save();
                return true;
            }
            else if (score > _data.BestScores[levelId])
            {
                _data.BestScores[levelId] = score;
                Save();
                return true;
            }

            return false;
        }

        public int GetBestScore(string levelId)
        {
            if(string.IsNullOrEmpty(levelId))
                return 0;
            
            return _data.BestScores.TryGetValue(levelId, out int best) ? best : 0;
        }

        public int GetLevelStars(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) || _data?.LevelStars == null)
                return 0;

            return _data.LevelStars.TryGetValue(levelId, out var stars) ? stars : 0;
        }

        /// <summary>Guarda a melhor quantidade de estrelas da fase (nunca diminui). Não salva.</summary>
        public bool RegisterLevelStars(string levelId, int stars)
        {
            if (string.IsNullOrEmpty(levelId))
                return false;

            stars = UnityEngine.Mathf.Clamp(stars, 0, 3);
            var previous = GetLevelStars(levelId);
            if (stars <= previous)
                return false;

            _data.LevelStars[levelId] = stars;
            return true;
        }

        public int TotalStars(New_GameplayCore.LevelSetSO set)
        {
            if (set?.levels == null) return 0;

            var total = 0;
            foreach (var level in set.levels)
                if (level != null)
                    total += GetLevelStars(level.LevelKey);
            return total;
        }
    }
}
