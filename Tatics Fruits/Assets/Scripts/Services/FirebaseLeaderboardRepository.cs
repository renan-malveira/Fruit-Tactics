using System;
using System.Collections.Generic;
using System.Linq;
using Firebase.Database;
using UnityEngine;

namespace Services
{
    public class FirebaseLeaderboardRepository : ILeaderboardRepository
    {
        private const string LeaderboardNode = "leaderboard";
        private readonly DatabaseReference _db;

        public FirebaseLeaderboardRepository()
        {
            _db = FirebaseDatabase.DefaultInstance.RootReference;
        }

        public void FetchTopScores(int limit, Action<List<LeaderboardEntry>> onSuccess, Action<Exception> onFailure)
        {
            _db.Child(LeaderboardNode)
                .OrderByChild("score")
                .LimitToLast(limit)
                .GetValueAsync()
                .ContinueWith(task =>
                {
                    try
                    {
                        if (task.IsFaulted)
                        {
                            Debug.LogError($"[FirebaseLeaderboard] FetchTopScores failed: {task.Exception?.Flatten().InnerException?.Message}");
                            onFailure?.Invoke(task.Exception);
                            return;
                        }

                        var entries = new List<LeaderboardEntry>();
                        var snapshot = task.Result;

                        if (snapshot == null || !snapshot.Exists)
                        {
                            onSuccess?.Invoke(entries);
                            return;
                        }

                        foreach (var child in snapshot.Children)
                        {
                            var entry = new LeaderboardEntry
                            {
                                playerId   = child.Key,
                                playerName = ReadString(child, "playerName", "?"),
                                score      = ReadInt(child, "score"),
                                updatedAt  = ReadLong(child, "updatedAt")
                            };
                            entries.Add(entry);
                        }

                        var ranked = entries.OrderByDescending(e => e.score).ToList();
                        for (int i = 0; i < ranked.Count; i++)
                            ranked[i].rank = i + 1;

                        onSuccess?.Invoke(ranked);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FirebaseLeaderboard] Parse error: {ex}");
                        onFailure?.Invoke(ex);
                    }
                });
        }

        public void SubmitScore(string playerId, string playerName, int score)
        {
            if (string.IsNullOrEmpty(playerId)) return;

            var entry = new Dictionary<string, object>
            {
                { "playerName", playerName },
                { "score",      score },
                { "updatedAt",  DateTime.UtcNow.Ticks }
            };

            _db.Child(LeaderboardNode)
                .Child(playerId)
                .UpdateChildrenAsync(entry)
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                        Debug.LogError($"[FirebaseLeaderboard] Submit failed: {task.Exception}");
                });
        }

        private static string ReadString(DataSnapshot child, string key, string fallback)
        {
            var val = child.Child(key).Value;
            return val != null ? val.ToString() : fallback;
        }

        private static int ReadInt(DataSnapshot child, string key)
        {
            var val = child.Child(key).Value;
            if (val == null) return 0;
            return int.TryParse(val.ToString(), out var result) ? result : 0;
        }

        private static long ReadLong(DataSnapshot child, string key)
        {
            var val = child.Child(key).Value;
            if (val == null) return 0L;
            return long.TryParse(val.ToString(), out var result) ? result : 0L;
        }
    }
}
