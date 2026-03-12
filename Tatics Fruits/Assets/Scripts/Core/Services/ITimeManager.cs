using System;
using System.Collections.Generic;
using Core.ScriptableObjects;
using New_GameplayCore;

namespace Core.Services
{
    public interface ITimeManager
    {
        int TimeLeftSeconds { get; }
        bool CanPay(int seconds);
        bool TryPay(int seconds);
        void Add(int seconds);
        void Pause();
        void Resume();
        event Action<int> OnTimeChanged;
        event Action<int> OnTimeDelta;
    }

    public interface IScoreService
    {
        int Total { get; }
        int BestCombo { get; }
        int CurrentCombo { get; }
        void AddPairScore(CardInstance A, CardInstance B, float comboMultiplier, out int pointsAdded);
        void ResetCombo();
        event Action<int, int> OnScoreChanged;
    }

    public interface IComboTracker
    {
        int CurrentCombo { get; }
        float RemainingWindowsMs { get; }
        ComboTierConfigSo.ComboTier CurrentTier { get; }
        ComboTierConfigSo TierConfig { get; }
        void RegisterPair();
        void Tick(float deltaMs);
        void Reset();
        event Action<int> OnComboChanged;
        event Action<int, ComboTierConfigSo.ComboTier> OnComboTierChanged;
    }

    public interface IDeckService
    {
        void Build(DeckConfigSo config, System.Random rng);
        bool TryDraw(out CardInstance card);
        int DrawMany(int count, IList<CardInstance> buffer);
        void Discard(CardInstance card);
        void DiscardMany(IReadOnlyList<CardInstance> cards);
        bool TryRefillFromDiscard();
        int DeckCount { get; }
        int DiscardCount { get; }
        
        int TotalInitialCount { get; }
        
        event System.Action<int, int> OnDeckChanged;
    }

    public interface IHandService
    {
        IReadOnlyList<CardInstance> Cards { get; }
        int MaxSize { get; }
        bool HasSpace { get; }
        bool TryAdd(CardInstance card);
        void AddMany(IReadOnlyList<CardInstance> cards);
        bool TryRemove(CardInstance card);
        int RemoveWhere(Predicate<CardInstance> predicate, IList<CardInstance> removedOut);
        void ClearTo(IList<CardInstance> movedOut);
        event Action<IReadOnlyList<CardInstance>> OnHandChanged;
    }

    public interface IRuleEngine
    {
        bool TryMakePair(CardInstance A, CardInstance B, out PairResult result);
        bool IsValidPair(CardInstance A, CardInstance B);
        event System.Action<PairResult> OnPairResolved;
        event System.Action OnInvalidPairAttempt;
    }

    public interface ISwapService
    {
        bool TrySwapAll();
        bool TrySwapRandom();
    }

    public interface ITelemetry
    {
        void Event(string name, IDictionary<string, object> data = null);
    }

    public interface IStorageService
    {
        void Save<T>(string key, T data);
        bool TryLoad<T>(string key, out T data);
    }
}