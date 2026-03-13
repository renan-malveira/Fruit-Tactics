using UnityEngine;

namespace Core.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Create LevelConfigSO", fileName = "LevelConfigSO", order = 0)]
    public class LevelConfigSo : ScriptableObject
    {
        public string levelId    = "fase_1";
        public string displayName = "Fase 1";
        [TextArea] public string description;

        public int targetScore       = 100;
        public int initialTimeSeconds = 60;
        public int handSize          = 6;
        public int deckSize          = 60;

        public int timeBonusOnPair      = 3;
        public int swapAllTimePenalty   = 5;
        public int swapRandomTimePenalty = 2;
        public bool allowEmptyDeckRefill = true;

        public DeckConfigSo deck;

        public bool useFixedSeed;
        public int  fixedSeed;

        public int   scorePerPairBase        = 100;
        public int   scorePerCardValueFactor = 10;

        public float   comboWindows      = 2500f;
        public float[] comboMultipliers  = { 1.0f, 1.2f, 1.5f, 2.0f };

        [Range(0f, 1f)] public float star1Threshold = 0.33f;
        [Range(0f, 1f)] public float star2Threshold = 0.66f;
        [Range(0f, 1f)] public float star3Threshold = 1.00f;

        [Header("Gold Rewards")]
        public int goldRewardStar1 = 10;
        public int goldRewardStar2 = 20;
        public int goldRewardStar3 = 40;

        public int GetGoldReward(int starsEarned)
        {
            return starsEarned switch
            {
                1 => goldRewardStar1,
                2 => goldRewardStar1 + goldRewardStar2,
                3 => goldRewardStar1 + goldRewardStar2 + goldRewardStar3,
                _ => 0
            };
        }
    }
}