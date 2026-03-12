using System;
using System.Collections.Generic;

namespace Services
{
    public class LeaderboardPresenter
    {
        private readonly ILeaderboardRepository _repository;
        private const int TopScoresLimit = 100;

        public event Action<List<LeaderboardEntry>> OnDataReady;
        public event Action OnLoading;
        public event Action<Exception> OnError;
        
        public LeaderboardPresenter(ILeaderboardRepository repository)
        {
            _repository = repository;
        }

        public void Fetch()
        {
            OnLoading?.Invoke();
            _repository.FetchTopScores(
                TopScoresLimit,
                entries =>
                {
                    MainThreadDispatcher.Enqueue(() => OnDataReady?.Invoke(entries));
                },
                ex =>
                {
                    MainThreadDispatcher.Enqueue(() => OnError?.Invoke(ex));
                }
            );
        }

        public void SubmitIfNewBest(string playerId, string playerName, int score, int previousBest)
        {
            if (score <= previousBest)
                return;
            
            _repository.SubmitScore(playerId, playerName, score);
        }
    }
}