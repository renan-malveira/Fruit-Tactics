using System;
using Core.ScriptableObjects;
using New_GameplayCore;
using UnityEngine;

namespace Core.Services
{
    public class RuleEngine : IRuleEngine
    {
        private readonly IHandService _hand;
        private readonly IDeckService _deck;
        private readonly IScoreService _score;
        private readonly ITimeManager _time;
        private readonly IComboTracker _combo;
        private readonly LevelConfigSo _cfg;

        public event Action<PairResult> OnPairResolved;
        public event Action OnInvalidPairAttempt;

        public RuleEngine(IHandService hand, IDeckService deck, IScoreService score, 
            ITimeManager time, IComboTracker combo, LevelConfigSo cfg)
        {
            _hand = hand;
            _deck = deck;
            _score = score;
            _time = time;
            _combo = combo;
            _cfg = cfg;
        }

        public bool IsValidPair(CardInstance a, CardInstance b)
            => a.Type.id == b.Type.id;

        public bool TryMakePair(CardInstance a, CardInstance b, out PairResult result)
        {
            result = default;
            if (!IsValidPair(a, b))
            {
                OnInvalidPairAttempt?.Invoke();
                return false;
            }

            _combo.RegisterPair();

            var comboIndex = Mathf.Clamp(_combo.CurrentCombo - 1, 0, _cfg.comboMultipliers.Length - 1);
            var multiplier = _cfg.comboMultipliers[comboIndex];
            
            var tier = _combo.CurrentTier;
            if (tier != null)
            {
                multiplier *= tier.scoreMultiplier;
            }

            _score.AddPairScore(a, b, multiplier, out var added);
            
            var pairValue = a.Type.baseValue;
            var bonus = pairValue;
            bonus += Mathf.Max(0, _combo.CurrentCombo - 1);
            
            if (tier != null)
            {
                bonus += tier.timeBonusExtra;
            }
            
            _time.Add(bonus);

            _hand.TryRemove(a);
            _hand.TryRemove(b);

            result = new PairResult(a, b, added, _combo.CurrentCombo, bonus);
            OnPairResolved?.Invoke(result);
            return true;
        }
    }
}
