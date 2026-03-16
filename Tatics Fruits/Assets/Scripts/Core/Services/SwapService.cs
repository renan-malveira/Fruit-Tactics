using System;
using System.Collections.Generic;
using Core.ScriptableObjects;
using New_GameplayCore;
using New_GameplayCore.Services;

namespace Core.Services
{
    public class SwapService : ISwapService
    {
        private readonly IHandService _hand;
        private readonly IDeckService _deck;
        private readonly ITimeManager _time;
        private readonly LevelConfigSo _cfg;

        public event Action<bool, int> OnSwapAllAttempted;
        public event Action<bool, int> OnSwapRandomAttempted;

        public SwapService(IHandService hand, IDeckService deck, ITimeManager time, LevelConfigSo cfg)
        {
            _hand = hand;
            _deck = deck;
            _time = time;
            _cfg  = cfg;
        }

        public bool TrySwapAll()
        {
            int penalty = _cfg.swapAllTimePenalty;

            if (!_time.CanPay(penalty) || _hand.Cards.Count == 0)
            {
                OnSwapAllAttempted?.Invoke(false, penalty);
                return false;
            }

            var currentCount = _hand.Cards.Count;
            _time.TryPay(penalty);

            var toDiscard = new List<CardInstance>(_hand.Cards);
            _hand.RemoveWhere(_ => true, toDiscard);
            _deck.DiscardMany(toDiscard);

            var newCards = new List<CardInstance>(currentCount);
            DrawUpTo(currentCount, newCards);
            _hand.AddMany(newCards);

            var success = newCards.Count > 0;
            OnSwapAllAttempted?.Invoke(success, penalty);
            return success;
        }

        public bool TrySwapRandom()
        {
            var penalty = _cfg.swapRandomTimePenalty;

            if (!_time.CanPay(penalty))
            {
                OnSwapRandomAttempted?.Invoke(false, penalty);
                return false;
            }

            if (_hand.Cards.Count == 0)
            {
                OnSwapRandomAttempted?.Invoke(false, penalty);
                return false;
            }

            var idx      = UnityEngine.Random.Range(0, _hand.Cards.Count);
            var toRemove = _hand.Cards[idx];
            
            bool drew = _deck.TryDraw(out var newCard);

            if (!drew && _cfg.allowEmptyDeckRefill && _deck.TryRefillFromDiscard())
                drew = _deck.TryDraw(out newCard);
            
            if (!drew)
            {
                OnSwapRandomAttempted?.Invoke(false, penalty);
                return false;
            }

            _time.TryPay(penalty);
            
            _deck.Discard(toRemove);

            if (_hand is HandService handService)
                handService.ReplaceAt(idx, newCard);
            else
            {
                _hand.TryRemove(toRemove);
                _hand.TryAdd(newCard);
            }

            OnSwapRandomAttempted?.Invoke(true, penalty);
            return true;
        }

        private void DrawUpTo(int target, IList<CardInstance> buffer)
        {
            var remaining = target;
            var drawn = _deck.DrawMany(remaining, buffer);
            remaining -= drawn;

            if (remaining > 0 && _cfg.allowEmptyDeckRefill && _deck.TryRefillFromDiscard())
            {
                drawn = _deck.DrawMany(remaining, buffer);
                remaining -= drawn;
            }
        }
    }
}
