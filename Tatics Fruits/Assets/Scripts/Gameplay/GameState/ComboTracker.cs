using System;
using Core.ScriptableObjects;
using Core.Services;
using New_GameplayCore;

namespace Gameplay.GameState
{
    public class ComboTracker : IComboTracker
    {
        private readonly LevelConfigSo _cfg;
        private readonly ComboTierConfigSo _tierConfig;
        private float _timer;
        private bool _active;
        private ComboTierConfigSo.ComboTier _currentTier;

        public int CurrentCombo { get; private set; }
        public float RemainingWindowsMs => _timer;
        public ComboTierConfigSo.ComboTier CurrentTier => _currentTier;
        public ComboTierConfigSo TierConfig => _tierConfig;

        public event Action<int> OnComboChanged;
        public event Action<int, ComboTierConfigSo.ComboTier> OnComboTierChanged;

        public ComboTracker(LevelConfigSo cfg, ComboTierConfigSo tierConfig = null)
        {
            _cfg        = cfg;
            _tierConfig = tierConfig;
        }

        public void RegisterPair()
        {
            CurrentCombo = (_active && _timer > 0) ? CurrentCombo + 1 : 1;
            _timer  = _cfg.comboWindows;
            _active = true;

            OnComboChanged?.Invoke(CurrentCombo);

            if (_tierConfig == null) return;

            var newTier    = _tierConfig.GetTierForCombo(CurrentCombo);
            var tierChanged = _currentTier == null || newTier.minComboCount != _currentTier.minComboCount;
            _currentTier   = newTier;

            OnComboTierChanged?.Invoke(CurrentCombo, _currentTier);
            
            if (tierChanged && CurrentCombo > 1)
                _currentTier = newTier;
        }

        public void Tick(float deltaMs)
        {
            if (!_active) return;

            _timer -= deltaMs;
            if (_timer > 0) return;

            _active      = false;
            CurrentCombo = 0;
            _currentTier = null;

            OnComboChanged?.Invoke(0);
            OnComboTierChanged?.Invoke(0, null);
        }

        public void Reset()
        {
            _active      = false;
            _timer       = 0;
            CurrentCombo = 0;
            _currentTier = null;

            OnComboChanged?.Invoke(0);
            OnComboTierChanged?.Invoke(0, null);
        }
    }
}
