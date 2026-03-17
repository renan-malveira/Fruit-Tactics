using System;
using System.Collections.Generic;
using Ads;
using Core.ScriptableObjects;
using Core.Services;
using DefaultNamespace.New_GameplayCore;
using New_GameplayCore;
using New_GameplayCore.GameState;
using New_GameplayCore.Services;
using UnityEngine;
using Random = System.Random;

namespace Gameplay.Controllers
{
    public class GameController : IGameController
    {
        private readonly IGameStateMachine _fsm;
        private readonly ITimeManager _time;
        private readonly IDeckService _deck;
        private readonly IHandService _hand;
        private readonly IRuleEngine _rule;
        private readonly ISwapService _swap;
        private readonly IComboTracker _combo;
        private LevelConfigSo _cfg;
        private readonly IScoreService _score;

        private CardInstance? _selectedCard = null;

        public event Action<EndCause> OnLevelEnded;
        public event Action<int, ComboTierConfigSo.ComboTier> OnComboTierChanged;
        public event Action<int> OnComboChanged;
        public event Action OnComboReset;
        public event Action<PairResult> OnPairResolved;
        public event Action OnEnterPreRound;
        public event Action OnExitPreRound;

        public bool IsPlaying => _fsm.Current == DefaultNamespace.New_GameplayCore.GameState.Playing;

        public GameController(IGameStateMachine fsm, ITimeManager time, IDeckService deck,
            IHandService hand, IRuleEngine rule, ISwapService swap, IComboTracker combo,
            LevelConfigSo cfg, ScoreService score)
        {
            _fsm = fsm;
            _time = time;
            _deck = deck;
            _hand = hand;
            _rule = rule;
            _swap = swap;
            _combo = combo ?? throw new ArgumentNullException(nameof(combo));
            _cfg = cfg;
            _score = score ?? throw new ArgumentNullException(nameof(score));

            _time.OnTimeChanged += t =>
            {
                if (t <= 0 && _fsm.Current == DefaultNamespace.New_GameplayCore.GameState.Playing)
                {
                    _fsm.SetState(DefaultNamespace.New_GameplayCore.GameState.Results);
                    OnLevelEnded?.Invoke(EndCause.TimeUp);
                }
            };

            _score.OnScoreChanged += (total, delta) =>
            {
                if (_fsm.Current == DefaultNamespace.New_GameplayCore.GameState.Playing && total >= _cfg.targetScore)
                {
                    _fsm.SetState(DefaultNamespace.New_GameplayCore.GameState.Results);
                    OnLevelEnded?.Invoke(EndCause.TargetReached);
                }
            };

            _combo.OnComboChanged += HandleComboChanged;
            _combo.OnComboTierChanged += HandleComboTierChanged;
            _rule.OnPairResolved += result => OnPairResolved?.Invoke(result);
        }

        private void HandleComboChanged(int comboCount)
        {
            OnComboChanged?.Invoke(comboCount);
            if (comboCount == 0)
                OnComboReset?.Invoke();
        }

        private void HandleComboTierChanged(int comboCount, ComboTierConfigSo.ComboTier tier)
        {
            OnComboTierChanged?.Invoke(comboCount, tier);
        }

        public void StartLevel(LevelConfigSo cfg, DeckConfigSo deckCfg)
        {
            _cfg = cfg;
            var rng = cfg.useFixedSeed ? new Random(cfg.fixedSeed) : new Random();
            _deck.Build(deckCfg, rng);
            _fsm.SetState(DefaultNamespace.New_GameplayCore.GameState.PreRound);
            OnEnterPreRound?.Invoke();
            var cards = new List<CardInstance>();
            _deck.DrawMany(cfg.handSize, cards);
            _hand.AddMany(cards);
        }

        public void UpdateTick(float deltaTime)
        {
            if (_fsm.Current != DefaultNamespace.New_GameplayCore.GameState.Playing)
                return;
            (_time as TimeManager)?.Tick(deltaTime);
            float deltaMs = deltaTime * 1000f;
            _combo.Tick(deltaMs);
        }

        public void OnCardSelected(CardInstance card)
        {
            if (_fsm.Current != DefaultNamespace.New_GameplayCore.GameState.Playing)
                return;

            if (_selectedCard == null)
            {
                _selectedCard = card;
                return;
            }

            if (_rule.TryMakePair(_selectedCard.Value, card, out _))
            {
                TryDrawReplacement();
                TryDrawReplacement();
            }

            _selectedCard = null;
        }

        private void TryDrawReplacement()
        {
            if (!_hand.HasSpace) return;

            if (_deck.TryDraw(out var drawn))
            {
                _hand.TryAdd(drawn);
                return;
            }

            if (_cfg.allowEmptyDeckRefill && _deck.TryRefillFromDiscard() && _deck.TryDraw(out drawn))
                _hand.TryAdd(drawn);
        }

        public void BeginPlayFromPreRound(PreRoundModel model)
        {
            _time.TryPay(_time.TimeLeftSeconds);
            _time.Add(_cfg.initialTimeSeconds);

            // Return the pre-round hand back to the deck before clearing,
            // so cards aren't permanently lost when the round begins.
            var returned = new System.Collections.Generic.List<CardInstance>();
            _hand.ClearTo(returned);
            foreach (var c in returned)
                _deck.Discard(c);

            var buf = new System.Collections.Generic.List<CardInstance>();
            _deck.DrawMany(_cfg.handSize, buf);
            _hand.AddMany(buf);
            _combo.Reset();
            OnExitPreRound?.Invoke();
            _fsm.SetState(DefaultNamespace.New_GameplayCore.GameState.Playing);
        }

        public void BackToLevelSelect()
        {
            _combo.Reset();
            _fsm.SetState(DefaultNamespace.New_GameplayCore.GameState.Boot);
        }

        public bool TryDrawOne()
        {
            if (!(_hand as HandService).HasSpace) return false;
            if (_deck.TryDraw(out var card)) return _hand.TryAdd(card);
            if (_cfg.allowEmptyDeckRefill && _deck.TryRefillFromDiscard() && _deck.TryDraw(out card))
                return _hand.TryAdd(card);
            return false;
        }

        public void OnSwapAllRequested() => _swap.TrySwapAll();
        public void OnSwapRandomRequested() => _swap.TrySwapRandom();
    }
}
