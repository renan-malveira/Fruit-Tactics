namespace Core
{
    /// <summary>
    /// Ponto único de configuração para publicação nas lojas.
    /// Tudo que muda entre Android e iOS, ou que precisa ser preenchido
    /// antes do release, fica aqui. Os menus "Tools/Release/Validar bloqueadores - Android/iOS"
    /// acusam qualquer valor vazio ou de teste.
    /// As páginas legais ficam no site da Suricatus (WordPress > File Organizer > jogos/fruittactics/);
    /// o original de cada uma está na raiz do repositório.
    /// </summary>
    public static class StoreConfig
    {
        // ---------------------------------------------------------------
        // Links legais (precisam ser HTTPS e estar publicados antes do envio)
        // ---------------------------------------------------------------

        /// <summary>Política de privacidade. Exigida pela Apple (5.1.1) dentro do app e pelo Google Play na ficha.</summary>
        public const string PrivacyPolicyUrl = "https://suricatusgames.com/jogos/fruittactics/privacidade.html";

        /// <summary>Página web para pedir exclusão de conta sem o app instalado (exigência do Google Play).</summary>
        public const string AccountDeletionUrl = "https://suricatusgames.com/jogos/fruittactics/exclusao-de-conta.html";

        public const string SupportEmail = "suricatusgames@gmail.com";

        // ---------------------------------------------------------------
        // AdMob — unidades de anúncio por plataforma
        // App IDs ficam em Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset
        // ---------------------------------------------------------------

        public const string AndroidInterstitialUnitId = "ca-app-pub-8609532543875876/3409474969";
        public const string AndroidBannerUnitId       = "ca-app-pub-8609532543875876/5682573689";

        // Criar o app iOS no AdMob e colar aqui as unidades próprias do iOS.
        // Enquanto estiverem vazias, o iOS não carrega anúncios (em vez de usar IDs de teste ou do Android).
        public const string IosInterstitialUnitId = "";
        public const string IosBannerUnitId       = "";

        public const string GoogleTestAdPrefix = "ca-app-pub-3940256099942544";

        public static string InterstitialUnitId
        {
            get
            {
#if UNITY_IOS
                return IosInterstitialUnitId;
#else
                return AndroidInterstitialUnitId;
#endif
            }
        }

        public static string BannerUnitId
        {
            get
            {
#if UNITY_IOS
                return IosBannerUnitId;
#else
                return AndroidBannerUnitId;
#endif
            }
        }

        public static bool IsUsableAdUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
                return false;

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            if (unitId.StartsWith(GoogleTestAdPrefix))
                return false;
#endif
            return true;
        }

        public static bool HasUrl(string url)
            => !string.IsNullOrEmpty(url) && url.StartsWith("https://");
    }
}
