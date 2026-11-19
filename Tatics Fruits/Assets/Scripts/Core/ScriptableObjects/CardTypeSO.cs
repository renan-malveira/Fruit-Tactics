using UnityEngine;

namespace Core.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Create CardTypeSO", fileName = "CardTypeSO", order = 0)]
    public class CardTypeSo : ScriptableObject
    {
        public string id;
        public Sprite sprite;
        public int baseValue = 1;
        public Rarity rarity;
    }

    public enum Rarity
    {
        Common,
        Rare,
        Epic
    }

    

    [System.Serializable]
    public struct PreRoundModel
    {
        public string levelId;
        public string displayName;
        public string description;

        public int targetScore;
        public int star1Score;
        public int star2Score;
        public int star3Score;

        public int initialTimeSec;
        public int handSize;
        public int timeBonusOnPair;
        public int swapAllPenalty;
        public int swapRandomPenalty;
        public bool allowRefillFromDiscard;

        public int deckTotalCount;
        public DeckEntrySummary[] composition;

        public int bestScore;

        public bool useFixedSeed;
        public int effectiveSeed;

        // v2.2: número da fase (título) e critérios das estrelas
        public int levelNumber;
        public int parTimeSeconds;
        public int bestStars;
    }

}