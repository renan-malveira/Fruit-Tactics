using UnityEngine;

namespace Core
{
    /// <summary>
    /// 60 FPS desde a primeira cena. No Android o padrão é 30 FPS e o ApplicationManager/MobileOptimizer,
    /// que pediam 60, não estão em nenhuma cena. Roda sozinho, sem precisar de objeto na cena.
    /// </summary>
    public static class FrameRateBootstrap
    {
        public const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // Com vSync ligado o targetFrameRate é ignorado; no mobile a tela já limita a taxa.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
