using Core.ScriptableObjects;
using UnityEngine;

namespace Core.Services
{
    /// <summary>
    /// Estrelas pela pontuação:
    /// 1 estrela  = star1Threshold da meta (1/3 da barra);
    /// 2 estrelas = star2Threshold da meta (2/3 da barra);
    /// 3 estrelas = barra cheia (a meta da fase).
    /// A fase termina na hora com a barra cheia. Com 1 ou 2 estrelas ela continua até acabar
    /// o tempo ou as cartas e termina em vitória; sem estrela nenhuma, em derrota.
    /// (Na v2.2 as estrelas vinham de meta, tempo-alvo e erros, e enchiam a barra com 1 estrela.)
    /// </summary>
    public static class StarRules
    {
        public static int Evaluate(LevelConfigSo cfg, int totalScore)
        {
            if (cfg == null || cfg.targetScore <= 0)
                return 0;

            if (totalScore >= ScoreFor(cfg, 3)) return 3;
            if (totalScore >= ScoreFor(cfg, 2)) return 2;
            if (totalScore >= ScoreFor(cfg, 1)) return 1;
            return 0;
        }

        /// <summary>Pontos que acendem a estrela 1, 2 ou 3.</summary>
        public static int ScoreFor(LevelConfigSo cfg, int star)
        {
            if (cfg == null)
                return 0;

            var target = Mathf.Max(1, cfg.targetScore);
            if (star >= 3)
                return target;

            var fraction = Mathf.Clamp01(star == 1 ? cfg.star1Threshold : cfg.star2Threshold);
            // Desconto mínimo para 300 x 0.33 dar 99 (e não 100 por arredondamento de float).
            return Mathf.Clamp(Mathf.CeilToInt(target * fraction - 0.001f), 1, target);
        }

        /// <summary>Posição da estrela na barra (0 a 1).</summary>
        public static float FractionFor(LevelConfigSo cfg, int star)
        {
            if (cfg == null || cfg.targetScore <= 0)
                return star / 3f;

            return ScoreFor(cfg, star) / (float)cfg.targetScore;
        }
    }
}
