using MoreMountains.Feedbacks;
using UnityEngine;

namespace Core.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Gameplay/Combo Tier Config", fileName = "ComboTierConfig", order = 1)]
    public class ComboTierConfigSo : ScriptableObject
    {
        [System.Serializable]
        public class ComboTier
        {
            public string tierName;
            public int    minComboCount;
            public Color  tierColor       = Color.white;
            public float  scoreMultiplier = 1f;
            public int    timeBonusExtra  = 0;

            [Header("Feel")]
            public GameObject tierVFXPrefab;
            public float      punchScale    = 0.18f;
            public float      punchDuration = 0.28f;
            public float      screenFlashAlpha = 0f;
            public Color      screenFlashColor = Color.white;
            public MMF_Player tierFeedback;
        }

        [Header("Combo Tier")]
        public ComboTier[] tiers =
        {
            new ComboTier { tierName = "Combo",      minComboCount = 1,  tierColor = Color.white,   scoreMultiplier = 1.0f, punchScale = 0.10f, punchDuration = 0.20f },
            new ComboTier { tierName = "Good!",      minComboCount = 3,  tierColor = Color.cyan,    scoreMultiplier = 1.5f, timeBonusExtra = 1, punchScale = 0.18f, punchDuration = 0.25f, screenFlashAlpha = 0.08f, screenFlashColor = Color.cyan    },
            new ComboTier { tierName = "Great!",     minComboCount = 5,  tierColor = Color.yellow,  scoreMultiplier = 2.0f, timeBonusExtra = 2, punchScale = 0.26f, punchDuration = 0.28f, screenFlashAlpha = 0.12f, screenFlashColor = Color.yellow  },
            new ComboTier { tierName = "Amazing!",   minComboCount = 8,  tierColor = Color.magenta, scoreMultiplier = 3.0f, timeBonusExtra = 3, punchScale = 0.36f, punchDuration = 0.32f, screenFlashAlpha = 0.16f, screenFlashColor = Color.magenta },
            new ComboTier { tierName = "LEGENDARY!", minComboCount = 12, tierColor = Color.red,     scoreMultiplier = 5.0f, timeBonusExtra = 5, punchScale = 0.50f, punchDuration = 0.40f, screenFlashAlpha = 0.22f, screenFlashColor = Color.red     },
        };

        public ComboTier GetTierForCombo(int comboCount)
        {
            ComboTier result = tiers[0];
            for (int i = tiers.Length - 1; i >= 0; i--)
            {
                if (comboCount >= tiers[i].minComboCount)
                {
                    result = tiers[i];
                    break;
                }
            }
            return result;
        }

        public int GetNextTierThreshold(int comboCount)
        {
            for (int i = 0; i < tiers.Length; i++)
            {
                if (tiers[i].minComboCount > comboCount)
                    return tiers[i].minComboCount;
            }
            return tiers[tiers.Length - 1].minComboCount;
        }
    }
}
