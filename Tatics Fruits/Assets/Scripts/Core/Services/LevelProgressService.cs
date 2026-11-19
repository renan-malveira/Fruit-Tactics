using System;
using Core.ScriptableObjects;
using Core.Services;
using UnityEngine;

namespace New_GameplayCore.Services
{
    /// <summary>
    /// Progresso das fases. Desde a v2.2 tudo fica no perfil do jogador (player_profile.json),
    /// que é o que vai para a nuvem: fase atual, maior fase liberada, recorde e estrelas por fase.
    /// O arquivo antigo levels_progress.json é lido uma vez e incorporado (ver MigrateLegacyFile).
    /// </summary>
    public class LevelProgressService : ILevelProgressService
    {
        private const string LegacyFile = "levels_progress.json";

        private readonly PlayerProfileService _profile;

        public LevelProgressService(PlayerProfileService profile)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public int CurrentIndex => Mathf.Max(0, _profile.Data.currentLevelIndex);
        public int UnlockedMaxIndex => Mathf.Max(0, _profile.Data.highestLevelUnlocked);

        public void Load()
        {
            if (_profile.Data == null)
                _profile.Load();

            MigrateLegacyFile();
        }

        public void Save() => _profile.Save();

        public void SetCurrentIndex(int idx)
        {
            _profile.Data.currentLevelIndex = Mathf.Max(0, idx);
            Save();
        }

        public LevelConfigSo Current(LevelSetSO set)
        {
            if (set == null || set.levels == null || set.levels.Length == 0)
                return null;

            var index = Mathf.Clamp(CurrentIndex, 0, set.levels.Length - 1);
            _profile.Data.currentLevelIndex = index;
            return set.levels[index];
        }

        /// <summary>Guarda recorde e estrelas da fase (só sobem) e salva.</summary>
        public void RecordResult(LevelConfigSo cfg, int totalScore, int stars)
        {
            if (cfg == null) return;

            var key = cfg.LevelKey;
            _profile.RegisterBestScore(key, totalScore);
            _profile.RegisterLevelStars(key, stars);
            Save();
        }

        /// <summary>Venceu quem bateu a meta (a fase só termina em vitória quando isso acontece).</summary>
        public bool CanAdvance(LevelConfigSo cfg, int totalScore, float unlockPct = 0.75f)
            => cfg != null && cfg.targetScore > 0 && totalScore >= cfg.targetScore;

        /// <summary>
        /// Vitória na fase <paramref name="levelIndex"/>: libera a próxima e já deixa ela como atual,
        /// para que "Menu" e depois "Jogar" continuem de onde o jogador parou.
        /// </summary>
        public void CompleteLevel(int levelIndex, LevelSetSO set)
        {
            var data = _profile.Data;
            data.highestLevelUnlocked = Mathf.Max(data.highestLevelUnlocked, levelIndex + 1);

            var last = set != null && set.levels != null ? set.levels.Length - 1 : levelIndex + 1;
            data.currentLevelIndex = Mathf.Clamp(levelIndex + 1, 0, Mathf.Max(0, last));
            Save();
        }

        public void Advance(LevelSetSO set)
        {
            if (set == null || set.levels == null)
                return;

            var data = _profile.Data;
            data.currentLevelIndex = Mathf.Min(CurrentIndex + 1, set.levels.Length - 1);
            data.highestLevelUnlocked = Mathf.Max(data.highestLevelUnlocked, data.currentLevelIndex);
            Save();
        }

        public void Replay() => Save();

        public void MarkNextUnlockedIfEligible(LevelConfigSo cfg, int totalScore, float unlockPct = 0.75f)
        {
            if (!CanAdvance(cfg, totalScore, unlockPct))
                return;

            _profile.Data.highestLevelUnlocked = Math.Max(_profile.Data.highestLevelUnlocked, CurrentIndex + 1);
            Save();
        }

        /// <summary>
        /// "Jogar de novo" depois da última fase: volta para a fase 1 sem apagar estrelas,
        /// recordes nem fases liberadas (antes isso zerava todo o progresso).
        /// </summary>
        public void Reset()
        {
            _profile.Data.currentLevelIndex = 0;
            Save();
        }

        public int TotalStars(LevelSetSO set) => _profile.TotalStars(set);

        private void MigrateLegacyFile()
        {
            var data = _profile.Data;
            if (data == null || data.levelProgressMigrated)
                return;

            if (JsonDataService.TryLoad(LegacyFile, out LevelProgressData legacy) && legacy != null)
            {
                // Mesmo critério do botão Jogar antigo: continua na fase mais avançada conhecida.
                var furthest = Mathf.Max(
                    Mathf.Max(data.currentLevelIndex, data.highestLevelUnlocked),
                    Mathf.Max(legacy.currentIndex, legacy.unlockedMaxIndex));

                data.currentLevelIndex = Mathf.Max(0, furthest);
                data.highestLevelUnlocked = Mathf.Max(data.highestLevelUnlocked, legacy.unlockedMaxIndex);
            }
            else
            {
                data.currentLevelIndex = Mathf.Max(data.currentLevelIndex, data.highestLevelUnlocked);
            }

            data.levelProgressMigrated = true;
            Save();
        }
    }
}
