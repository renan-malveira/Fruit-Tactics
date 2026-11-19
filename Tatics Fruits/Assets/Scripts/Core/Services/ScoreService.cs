using System;
using Core.ScriptableObjects;
using Core.Services;
using Managers;
using UnityEngine;

namespace New_GameplayCore.Services
{
    public class ScoreService : IScoreService
    {
        private readonly LevelConfigSo _cfg;
        private int _total;
        private int _currentCombo;
        private int _bestCombo;

        public int Total => _total;
        public int CurrentCombo => _currentCombo;
        public int BestCombo => _bestCombo;
        
        public event Action<int, int> OnScoreChanged;

        public ScoreService(LevelConfigSo cfg)
        {
            _cfg = cfg;
        }

        /// <summary>
        /// Pontos do par = (base da fase + valor das duas cartas) x multiplicador do combo.
        /// O multiplicador vem do tier atual (Combo x1, Bom! x2 ... Lenda!! x5).
        /// </summary>
        public void AddPairScore(CardInstance A, CardInstance B, float multiplier, out int pointsAdded)
        {
            var basePoints = Mathf.Max(0, _cfg.scorePerPairBase) + A.Value + B.Value;
            var safeMultiplier = multiplier > 0f ? multiplier : 1f;
            pointsAdded = Mathf.Max(1, Mathf.RoundToInt(basePoints * safeMultiplier));
            _total += pointsAdded;
            OnScoreChanged?.Invoke(_total, pointsAdded);
        }

        public void ResetCombo()
        {
            _currentCombo = 0;
        }

        public void RegisterCombo()
        {
            _currentCombo++;
            _bestCombo = Mathf.Max(_bestCombo, _currentCombo);
            
            if (_currentCombo >= 3)
            {
                AnalyticsManager.Instance?.TrackCombo(_currentCombo, _total);
            }
        }
    }
}
