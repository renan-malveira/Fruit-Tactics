using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Editor
{
    /// <summary>
    /// Configurações de publicação (Google Play + App Store) aplicadas pela API do Editor,
    /// para não editar ProjectSettings.asset por fora com o Unity aberto.
    ///
    /// Menu:
    ///   Tools/Release/1. Aplicar configurações de loja
    ///   Tools/Release/2. Validar bloqueadores - Android
    ///   Tools/Release/3. Validar bloqueadores - iOS
    /// Cada loja é validada à parte: o envio do Android não depende das pendências do iOS.
    /// </summary>
    public static class StoreReleaseSetup
    {
        // -----------------------------------------------------------------
        // Valores desta versão
        // -----------------------------------------------------------------
        private const string VersionName = "2.2";
        // 43 foi enviado em 20/11/2026 e barrado na revisão (tela em retrato obrigatória).
        // O Play não aceita repetir uma versionCode já enviada.
        private const int MinAndroidVersionCode = 44;
        private const int AndroidTargetApi = 36;        // exigência do Google Play para atualizações
        // App já publicado na App Store (id6761233443, versão 1.0 de 31/03/2026). Outro bundle ID
        // criaria um app novo em vez de atualizar este.
        private const string IosBundleId = "com.suricatusgames.fruittactics.mobile";
        // Ícone do iOS: 1024x1024 sem transparência (a App Store recusa canal alfa no ícone grande).
        private const string IosIconPath = "Assets/Sprite/AppIcon_iOS.png";
        // Vazio: o time é escolhido no Xcode (Signing & Capabilities > Team).
        // Preencher (Apple Developer > Membership > Team ID) dispensa esse passo.
        private const string IosTeamId = "";
        // App iOS no AdMob, ligado ao id6761233443 (o do Android é ~3807758387).
        private const string IosAdMobAppId = "ca-app-pub-8609532543875876~6844724801";

        private const string AttDescription =
            "Usamos essa permissão para mostrar anúncios mais relevantes e manter o Fruit Tactics gratuito.";

        private const string AdMobSettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string GoogleServicesJsonPath = "Assets/Plugins/Android/google-services.json";
        private const string AndroidPluginsRoot = "Assets/Plugins/Android";

        private static readonly string[] ForbiddenAndroidPermissions =
        {
            "android.permission.ACCESS_COARSE_LOCATION",
            "android.permission.ACCESS_FINE_LOCATION",
            "android.permission.BLUETOOTH",
            "android.permission.BLUETOOTH_ADMIN",
            "android.permission.CHANGE_WIFI_STATE",
        };

        // -----------------------------------------------------------------
        // 1. Aplicar
        // -----------------------------------------------------------------

        [MenuItem("Tools/Release/1. Aplicar configurações de loja")]
        public static void Apply()
        {
            var log = new List<string>();

            PlayerSettings.bundleVersion = VersionName;
            log.Add($"Versão: {VersionName}");

            var code = Math.Max(PlayerSettings.Android.bundleVersionCode, MinAndroidVersionCode);
            PlayerSettings.Android.bundleVersionCode = code;
            log.Add($"Android versionCode: {code}");

            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)AndroidTargetApi;
            log.Add($"Android targetSdk: {AndroidTargetApi}");

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, IosBundleId);
            log.Add($"iOS bundle ID: {IosBundleId}");

            if (!int.TryParse(PlayerSettings.iOS.buildNumber, out var build) || build < 1)
                PlayerSettings.iOS.buildNumber = "1";
            log.Add($"iOS build number: {PlayerSettings.iOS.buildNumber}");

            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            if (!string.IsNullOrEmpty(IosTeamId))
                PlayerSettings.iOS.appleDeveloperTeamID = IosTeamId;

            // A 1.0 saiu para iPhone e iPad; uma atualização não pode deixar de suportar iPad.
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            log.Add("iOS: iPhone + iPad");

            // Sem pedido de notificação na abertura: ele disputava a tela com o ATT, que às vezes
            // nem aparecia. A permissão é pedida depois dos termos, do consentimento e do ATT.
            Unity.Notifications.NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = false;
            log.Add("iOS: notificações não pedem permissão na abertura");

#if UNITY_IOS
            ApplyIosIcons(log);
#else
            log.Add("iOS: ícones só são aplicados com a plataforma iOS ativa (rode de novo no Mac)");
#endif

            if (SetAdMobString("adMobIOSAppId", IosAdMobAppId))
                log.Add($"iOS: App ID do AdMob {IosAdMobAppId}");

            if (SetAdMobString("userTrackingUsageDescription", AttDescription))
                log.Add("iOS: texto do ATT (NSUserTrackingUsageDescription) definido");

            if (RegenerateGpgsManifestWithoutNearby())
                log.Add("GPGS: manifest regerado sem Nearby Connections");

            AssetDatabase.SaveAssets();

            Debug.Log("[StoreReleaseSetup] Aplicado:\n  - " + string.Join("\n  - ", log));
            EditorUtility.DisplayDialog("Configurações de loja",
                "Aplicado:\n\n• " + string.Join("\n• ", log) +
                "\n\nRode \"2. Validar bloqueadores - Android\" (ou \"3. ... - iOS\") para ver o que ainda falta.", "OK");
        }

        // -----------------------------------------------------------------
        // 2. Validar
        // -----------------------------------------------------------------

        [MenuItem("Tools/Release/2. Validar bloqueadores - Android")]
        public static void ValidateAndroidRelease() => RunValidation("Android", ValidateAndroid);

        [MenuItem("Tools/Release/3. Validar bloqueadores - iOS")]
        public static void ValidateIosRelease() => RunValidation("iOS", ValidateIos);

        private static void RunValidation(string platform, Action<List<string>, List<string>> validatePlatform)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            validatePlatform(errors, warnings);
            ValidateCommon(errors, warnings);

            // --- Resultado --------------------------------------------------
            foreach (var e in errors) Debug.LogError($"[Loja {platform}] ❌ " + e);
            foreach (var w in warnings) Debug.LogWarning($"[Loja {platform}] ⚠️ " + w);

            var summary = errors.Count == 0
                ? $"{platform}: nenhum bloqueador. {warnings.Count} aviso(s)."
                : $"{platform}: {errors.Count} bloqueador(es) e {warnings.Count} aviso(s). Detalhes no Console.";

            if (errors.Count == 0) Debug.Log($"[Loja {platform}] ✅ " + summary);

            EditorUtility.DisplayDialog($"Bloqueadores de loja - {platform}", summary +
                (errors.Count > 0 ? "\n\n• " + string.Join("\n• ", errors.Take(12)) : string.Empty), "OK");
        }

        private static void ValidateAndroid(List<string> errors, List<string> warnings)
        {
            var target = (int)PlayerSettings.Android.targetSdkVersion;
            if (target == 0) errors.Add("Android: targetSdk em automático. Fixe em 36.");
            else if (target < AndroidTargetApi) errors.Add($"Android: targetSdk {target} < {AndroidTargetApi}.");

            if (PlayerSettings.Android.bundleVersionCode < MinAndroidVersionCode)
                errors.Add($"Android: versionCode {PlayerSettings.Android.bundleVersionCode} precisa ser >= {MinAndroidVersionCode} (rode \"1. Aplicar\").");

            if (IsPortraitOnly() && !HasOptionalPortraitFeature())
                errors.Add("Android: sem <uses-feature android.hardware.screen.portrait required=\"false\">, " +
                           "o Play trata a tela em retrato como obrigatória e barra o envio.");

            if (!File.Exists(GoogleServicesJsonPath))
                errors.Add("Android: falta google-services.json.");

            foreach (var hit in FindForbiddenPermissions())
                errors.Add($"Android: permissão indevida {hit}");

            if (!string.IsNullOrEmpty(ReadGpgsSetting("App.NearbdServiceId")))
                errors.Add("GPGS: Nearby Connections ainda configurado (ProjectSettings/GooglePlayGameSettings.txt).");

            if (!StoreConfig.IsUsableAdUnit(StoreConfig.AndroidInterstitialUnitId) ||
                StoreConfig.AndroidInterstitialUnitId.StartsWith(StoreConfig.GoogleTestAdPrefix))
                errors.Add("Android: unidade de intersticial vazia ou de teste (StoreConfig).");

            if (!StoreConfig.IsUsableAdUnit(StoreConfig.AndroidBannerUnitId) ||
                StoreConfig.AndroidBannerUnitId.StartsWith(StoreConfig.GoogleTestAdPrefix))
                errors.Add("Android: unidade de banner vazia ou de teste (StoreConfig).");

            var keystore = PlayerSettings.Android.keystoreName ?? string.Empty;
            if (keystore.Contains("{inproject}") || keystore.StartsWith("Assets") || !Path.IsPathRooted(keystore))
                warnings.Add("Android: o keystore está dentro do projeto. Mantenha-o fora do Git.");
        }

        private static bool IsPortraitOnly()
        {
            var orientation = PlayerSettings.defaultInterfaceOrientation;
            if (orientation == UIOrientation.Portrait || orientation == UIOrientation.PortraitUpsideDown)
                return true;

            return orientation == UIOrientation.AutoRotation &&
                   !PlayerSettings.allowedAutorotateToLandscapeLeft &&
                   !PlayerSettings.allowedAutorotateToLandscapeRight;
        }

        /// <summary>Algum manifesto em Plugins/Android declara a tela em retrato como opcional?</summary>
        private static bool HasOptionalPortraitFeature()
        {
            if (!Directory.Exists(AndroidPluginsRoot))
                return false;

            foreach (var path in Directory.GetFiles(AndroidPluginsRoot, "AndroidManifest.xml", SearchOption.AllDirectories))
            {
                var xml = File.ReadAllText(path);
                var at = xml.IndexOf("android.hardware.screen.portrait", StringComparison.Ordinal);
                if (at < 0) continue;

                var start = xml.LastIndexOf("<uses-feature", at, StringComparison.Ordinal);
                var end = xml.IndexOf("/>", at, StringComparison.Ordinal);
                if (start < 0 || end < 0) continue;

                if (xml.Substring(start, end - start).Contains("android:required=\"false\""))
                    return true;
            }

            return false;
        }

        private static void ValidateIos(List<string> errors, List<string> warnings)
        {
            var iosId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
            if (iosId != IosBundleId)
                errors.Add($"iOS: bundle ID {iosId}; o app da App Store é {IosBundleId} (rode \"1. Aplicar\").");

            if (PlayerSettings.iOS.targetDevice != iOSTargetDevice.iPhoneAndiPad)
                errors.Add("iOS: a 1.0 suporta iPad; a atualização precisa ser iPhone + iPad (rode \"1. Aplicar\").");

            if (Unity.Notifications.NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch)
                errors.Add("iOS: notificações pedem permissão na abertura e atrapalham o pedido do ATT (rode \"1. Aplicar\").");

#if UNITY_IOS
            if (!HasIosMarketingIcon())
                errors.Add("iOS: falta o ícone da App Store (1024). Rode \"1. Aplicar\" com a plataforma iOS ativa.");
#endif

            if (string.IsNullOrEmpty(PlayerSettings.iOS.appleDeveloperTeamID))
                warnings.Add("iOS: Team ID vazio. No Xcode, escolha o time em Signing & Capabilities antes do Archive.");

            var plist = AssetDatabase.FindAssets("GoogleService-Info").Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => p.EndsWith("GoogleService-Info.plist"));
            if (plist == null)
                errors.Add("iOS: falta GoogleService-Info.plist (app iOS do Firebase, em Assets/).");
            else if (!File.ReadAllText(plist).Contains($"<string>{IosBundleId}</string>"))
                errors.Add($"iOS: {plist} não é do bundle {IosBundleId}. Baixe o do app iOS no Firebase.");

            var androidAppId = GetAdMobString("adMobAndroidAppId");
            var iosAppId = GetAdMobString("adMobIOSAppId");
            if (string.IsNullOrEmpty(iosAppId))
                errors.Add("iOS: App ID do AdMob vazio (rode \"1. Aplicar\").");
            else if (iosAppId == androidAppId)
                errors.Add("iOS: App ID do AdMob é o mesmo do Android (rode \"1. Aplicar\").");
            else if (iosAppId != IosAdMobAppId)
                errors.Add($"iOS: App ID do AdMob {iosAppId}; o do app iOS é {IosAdMobAppId} (rode \"1. Aplicar\").");

            if (string.IsNullOrEmpty(GetAdMobString("userTrackingUsageDescription")))
                errors.Add("iOS: texto do ATT vazio (rode \"1. Aplicar\").");

            if (!StoreConfig.IsUsableAdUnit(StoreConfig.IosInterstitialUnitId) ||
                StoreConfig.IosInterstitialUnitId.StartsWith(StoreConfig.GoogleTestAdPrefix))
                errors.Add("iOS: unidade de intersticial vazia ou de teste (StoreConfig).");

            if (!StoreConfig.IsUsableAdUnit(StoreConfig.IosBannerUnitId) ||
                StoreConfig.IosBannerUnitId.StartsWith(StoreConfig.GoogleTestAdPrefix))
                errors.Add("iOS: unidade de banner vazia ou de teste (StoreConfig).");

            if (!int.TryParse(PlayerSettings.iOS.buildNumber, out var build) || build < 1)
                errors.Add("iOS: build number precisa ser >= 1.");
        }

        private static void ValidateCommon(List<string> errors, List<string> warnings)
        {
            if (!StoreConfig.HasUrl(StoreConfig.PrivacyPolicyUrl))
                errors.Add("Política de privacidade: URL HTTPS vazia (StoreConfig.PrivacyPolicyUrl).");

            if (!StoreConfig.HasUrl(StoreConfig.AccountDeletionUrl))
                errors.Add("Exclusão de conta: URL HTTPS vazia (StoreConfig.AccountDeletionUrl).");

            if (PlayerSettings.bundleVersion != VersionName)
                warnings.Add($"Versão atual {PlayerSettings.bundleVersion}; esperado {VersionName}.");
        }

        // -----------------------------------------------------------------
        // iOS: pós-processamento do projeto Xcode
        // -----------------------------------------------------------------

        [PostProcessBuild(900)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
#if UNITY_IOS
            if (buildTarget != BuildTarget.iOS) return;

            // Framework do ATT (usado por Assets/Plugins/iOS/FTTrackingAuthorization.mm)
            var projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            var frameworkTarget = proj.GetUnityFrameworkTargetGuid();
            proj.AddFrameworkToProject(frameworkTarget, "AppTrackingTransparency.framework", true);
            proj.WriteToFile(projPath);

            var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            // Firebase Analytics começa desligado; ConsentManager liga após consentimento.
            plist.root.SetBoolean("FIREBASE_ANALYTICS_COLLECTION_ENABLED", false);

            // Só HTTPS padrão do sistema: isento de criptografia de exportação.
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);

            plist.WriteToFile(plistPath);
#endif
        }

#if UNITY_IOS
        private static readonly PlatformIconKind[] IosIconKinds =
        {
            UnityEditor.iOS.iOSPlatformIconKind.Application,
            UnityEditor.iOS.iOSPlatformIconKind.Spotlight,
            UnityEditor.iOS.iOSPlatformIconKind.Settings,
            UnityEditor.iOS.iOSPlatformIconKind.Notification,
            UnityEditor.iOS.iOSPlatformIconKind.Marketing,
        };

        /// <summary>Usa AppIcon_iOS.png (sem transparência) em todos os tamanhos de ícone do iOS.</summary>
        private static void ApplyIosIcons(List<string> log)
        {
            if (AssetImporter.GetAtPath(IosIconPath) is not TextureImporter importer)
            {
                log.Add($"iOS: ícone não encontrado em {IosIconPath}");
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(IosIconPath);
            foreach (var kind in IosIconKinds)
            {
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
                foreach (var icon in icons)
                    icon.SetTexture(texture);
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, icons);
            }

            log.Add("iOS: ícones (app, Spotlight, Ajustes, notificação e App Store) = AppIcon_iOS.png");
        }

        private static bool HasIosMarketingIcon()
        {
            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, UnityEditor.iOS.iOSPlatformIconKind.Marketing);
            return icons.Length > 0 && icons.All(i => i.GetTexture() != null);
        }
#endif

        // -----------------------------------------------------------------
        // Auxiliares
        // -----------------------------------------------------------------

        private static bool SetAdMobString(string field, string value)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AdMobSettingsPath);
            if (asset == null)
            {
                Debug.LogWarning($"[StoreReleaseSetup] {AdMobSettingsPath} não encontrado.");
                return false;
            }

            var so = new SerializedObject(asset);
            var prop = so.FindProperty(field);
            if (prop == null) return false;

            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return true;
        }

        private static string GetAdMobString(string field)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AdMobSettingsPath);
            if (asset == null) return string.Empty;
            var prop = new SerializedObject(asset).FindProperty(field);
            return prop?.stringValue ?? string.Empty;
        }

        private static bool RegenerateGpgsManifestWithoutNearby()
        {
            try
            {
                if (!string.IsNullOrEmpty(ReadGpgsSetting("App.NearbdServiceId")))
                {
                    Debug.LogWarning("[StoreReleaseSetup] Remova App.NearbdServiceId de ProjectSettings/GooglePlayGameSettings.txt antes.");
                    return false;
                }

                // Recarrega do disco para o plugin não regravar o Nearby que estava em memória.
                GooglePlayGames.Editor.GPGSProjectSettings.Reload();
                GooglePlayGames.Editor.GPGSUtil.GenerateAndroidManifest();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StoreReleaseSetup] Não foi possível regerar o manifest do GPGS: {e.Message}");
                return false;
            }
        }

        private static string ReadGpgsSetting(string key)
        {
            const string path = "ProjectSettings/GooglePlayGameSettings.txt";
            if (!File.Exists(path)) return string.Empty;

            foreach (var line in File.ReadAllLines(path))
            {
                var idx = line.IndexOf('=');
                if (idx > 0 && line.Substring(0, idx) == key)
                    return line.Substring(idx + 1).Trim();
            }
            return string.Empty;
        }

        private static IEnumerable<string> FindForbiddenPermissions()
        {
            if (!Directory.Exists(AndroidPluginsRoot)) yield break;

            foreach (var file in Directory.GetFiles(AndroidPluginsRoot, "AndroidManifest.xml", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                foreach (var perm in ForbiddenAndroidPermissions)
                {
                    if (text.Contains($"\"{perm}\""))
                        yield return $"{perm} em {file.Replace('\\', '/')}";
                }
            }
        }
    }
}
