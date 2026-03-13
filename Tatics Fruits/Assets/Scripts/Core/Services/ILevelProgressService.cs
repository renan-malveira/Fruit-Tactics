using Core.ScriptableObjects;
using New_GameplayCore;

namespace Core.Services
{
    public interface ILevelProgressService
    {
        int CurrentIndex { get; }
        int UnlockedMaxIndex { get; }
        LevelConfigSo Current(LevelSetSO set);
        void RecordResult(LevelConfigSo cfg, int totalScore, int stars);
        bool CanAdvance(LevelConfigSo cfg, int totalScore, float unlockPct = 0.75f);
        void Advance(LevelSetSO set);
        void Replay();
        void Save();
        void Load();
        void Reset();
        int TotalStars(LevelSetSO set);
    }
}