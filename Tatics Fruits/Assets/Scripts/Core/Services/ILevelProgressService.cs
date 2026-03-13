using Core.ScriptableObjects;
using New_GameplayCore;

namespace Core.Services
{
    public interface ILevelProgressService
    {
        int CurrentIndex { get; }
        int UnlockedMaxIndex { get; }
        LevelConfigSO Current(LevelSetSO set);
        void RecordResult(LevelConfigSO cfg, int totalScore, int stars);
        bool CanAdvance(LevelConfigSO cfg, int totalScore, float unlockPct = 0.75f);
        void Advance(LevelSetSO set);
        void Replay();
        void Save();
        void Load();
        void Reset();
        int TotalStars(LevelSetSO set);
    }
}