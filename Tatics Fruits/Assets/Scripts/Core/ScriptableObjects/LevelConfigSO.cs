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

        [Tooltip("Janela do combo em milissegundos: o próximo par precisa sair antes disso.")]
        public float   comboWindows      = 2500f;
        [Tooltip("Não usado desde a v2.2: o multiplicador vem do ComboTierConfig.")]
        public float[] comboMultipliers  = { 1.0f, 1.2f, 1.5f, 2.0f };

        [Header("Estrelas (v2.2)")]
        [Tooltip("Não usado: as estrelas vêm da pontuação (star1Threshold, star2Threshold e barra cheia).")]
        public int parTimeSeconds = 0;

        [Tooltip("Fração da meta que dá 1 estrela e passa de fase (0.33 = 1/3 da barra).")]
        [Range(0f, 1f)] public float star1Threshold = 0.33f;
        [Tooltip("Fração da meta que dá 2 estrelas (0.66 = 2/3 da barra).")]
        [Range(0f, 1f)] public float star2Threshold = 0.66f;
        [Tooltip("Não usado: 3 estrelas é sempre a barra cheia (a meta), que também termina a fase.")]
        [Range(0f, 1f)] public float star3Threshold = 1.00f;

        [Header("Gold Rewards")]
        public int goldRewardStar1 = 10;
        public int goldRewardStar2 = 20;
        public int goldRewardStar3 = 40;

        public string LevelKey => string.IsNullOrEmpty(levelId) ? name : levelId;

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