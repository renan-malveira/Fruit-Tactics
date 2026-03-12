[System.Serializable]
public class LeaderboardEntry
{
    public int rank;
    public string playerId;
    public string playerName;
    public int score;
    public float timeSeconds;
    public long updatedAt;
}

[System.Serializable]
public class LeaderboardPayLoad
{
    public LeaderboardEntry[] daily;
    public LeaderboardEntry[] weekly;
    public LeaderboardEntry[] allTime;
}