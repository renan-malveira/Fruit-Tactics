using System;
using Core.ScriptableObjects;
using DefaultNamespace.New_GameplayCore;
using New_GameplayCore;

namespace Core.Services
{
    public interface IGameController
    {
        bool IsPlaying { get; }
        event Action OnEnterPreRound;
        event Action OnExitPreRound;
        event Action<EndCause> OnLevelEnded;
        event Action<PairResult> OnPairResolved;
        void StartLevel(LevelConfigSO cfg, DeckConfigSo deckCfg);
        void UpdateTick(float deltaTime);
        void OnCardSelected(CardInstance card);
        void BeginPlayFromPreRound(PreRoundModel model);
        void BackToLevelSelect();
        void OnSwapAllRequested();
        void OnSwapRandomRequested();
    }
}