using Core.ScriptableObjects;
using UnityEngine;

namespace Core.Services
{
    /// <summary>
    /// Estrelas da v2.2 (meta, tempo e erros):
    /// 1 estrela  = bater a meta de pontos;
    /// 2 estrelas = bater a meta dentro do tempo-alvo da fase (LevelConfigSo.parTimeSeconds);
    /// 3 estrelas = as duas coisas sem nenhum par errado.
    /// Fase sem tempo-alvo (parTimeSeconds = 0) usa "terminar com 25% do tempo inicial sobrando".
    /// </summary>
    public static class StarRules
    {
        public const float FallbackTimeLeftRatio = 0.25f;

        public static int Evaluate(LevelConfigSo cfg, bool reachedTarget, float elapsedSeconds, int timeLeftSeconds, int mistakes)
        {
            if (cfg == null || !reachedTarget)
                return 0;

            if (!MetTimeGoal(cfg, elapsedSeconds, timeLeftSeconds))
                return 1;

            return mistakes <= 0 ? 3 : 2;
        }

        public static bool MetTimeGoal(LevelConfigSo cfg, float elapsedSeconds, int timeLeftSeconds)
        {
            if (cfg == null)
                return false;

            if (cfg.parTimeSeconds > 0)
                return elapsedSeconds <= cfg.parTimeSeconds;

            return timeLeftSeconds >= FallbackTimeLeftSeconds(cfg);
        }

        public static int FallbackTimeLeftSeconds(LevelConfigSo cfg)
            => cfg == null ? 0 : Mathf.CeilToInt(cfg.initialTimeSeconds * FallbackTimeLeftRatio);
    }
}
