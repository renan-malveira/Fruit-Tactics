using System;

namespace DefaultNamespace.New_GameplayCore
{
    public enum GameState {Boot, PreRound, Countdown, Playing, Results}
    public enum EndCause{TargetReached, TimeUp, OutOfCards}
    public interface IGameStateMachine
    {
        GameState Current { get; }
        void SetState(GameState state);
        event Action<GameState> OnStateChange;
    }
}