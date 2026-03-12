using System;
using System.Collections.Generic;

namespace Services
{
    public interface ILeaderboardRepository
    {
        void FetchTopScores(int limit, Action<List<LeaderboardEntry>> onSuccess, Action<Exception> onFailure);
        void SubmitScore(string playerId, string playerName, int score);
    }
}