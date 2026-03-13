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
        public float RemainingWindowsMs  => _timer;
        public ComboTierConfigSo.ComboTier CurrentTier => _currentTier;
        public ComboTierConfigSo TierConfig => _tierConfig;
        public event Action<int> OnComboChanged;
        public event Action<int, ComboTierConfigSo.ComboTier> OnComboTierChanged;

        public ComboTracker(LevelConfigSo cfg, ComboTierConfigSo tierConfig = null)
        {
            _cfg = cfg;
            _tierConfig = tierConfig;
            UnityEngine.Debug.Log($"[ComboTracker] Created with comboWindows={cfg?.comboWindows ?? 0}ms, tierConfig={(tierConfig != null ? "assigned" : "null")}");
        }

        public void RegisterPair()
        {
            var previousCombo = CurrentCombo;

            if (_active && _timer > 0)
                CurrentCombo++;
            else
                CurrentCombo = 1;

            _timer = _cfg.comboWindows;
            _active = true;
            
            UnityEngine.Debug.Log($"[ComboTracker] RegisterPair - Previous: {previousCombo}, New: {CurrentCombo}, Timer: {_timer}ms, Active: {_active}, WindowConfig: {_cfg.comboWindows}ms");
            UnityEngine.Debug.Log($"[ComboTracker] RegisterPair - About to invoke OnComboChanged event with value: {CurrentCombo}");
            
            OnComboChanged?.Invoke(CurrentCombo);
            
            UnityEngine.Debug.Log($"[ComboTracker] RegisterPair - OnComboChanged event invoked successfully");

            if (_tierConfig != null)
            {
                var newTier = _tierConfig.GetTierForCombo(CurrentCombo);
                var tierChanged = _currentTier == null || newTier.minComboCount != _currentTier.minComboCount;

                _currentTier = newTier;

                if (tierChanged || CurrentCombo == 1)
                {
                    UnityEngine.Debug.Log($"[ComboTracker] RegisterPair - Invoking OnComboTierChanged: combo={CurrentCombo}, tier={newTier?.tierName ?? "null"}");
                    OnComboTierChanged?.Invoke(CurrentCombo, CurrentTier);
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("[ComboTracker] RegisterPair - No TierConfig assigned!");
            }
        }

        public void Tick(float deltaMs)
        {
            if(!_active)
                return;
            
            _timer -= deltaMs;
            if (_timer <= 0)
            {
                UnityEngine.Debug.Log($"[ComboTracker] Tick - Timer expired! Was combo {CurrentCombo}, resetting to 0. Timer went from {_timer + deltaMs}ms to {_timer}ms");
                _active = false;
                CurrentCombo = 0;
                _currentTier = null;
                UnityEngine.Debug.Log("[ComboTracker] Tick - About to invoke OnComboChanged(0)");
                OnComboChanged?.Invoke(CurrentCombo);
                OnComboTierChanged?.Invoke(0, null);
            }
        }

        public void Reset()
        {
            UnityEngine.Debug.Log("[ComboTracker] Reset called - Clearing combo state");
            UnityEngine.Debug.LogWarning("[ComboTracker] Reset called - Stack trace:");
            UnityEngine.Debug.LogWarning(UnityEngine.StackTraceUtility.ExtractStackTrace());
            _active = false;
            _timer = 0;
            CurrentCombo = 0;
            _currentTier = null;
            OnComboChanged?.Invoke(CurrentCombo);
            OnComboTierChanged?.Invoke(0, null);
        }
        
    }
}