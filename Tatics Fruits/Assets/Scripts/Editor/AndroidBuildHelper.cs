using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Editor
{
    public static class AndroidBuildHelper
    {
        [MenuItem("Tools/Android/Quick Setup for Release")]
        public static void QuickSetupForRelease()
        {
            Debug.Log("=== ANDROID RELEASE SETUP ===");

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            Debug.Log("✅ Min SDK: Android 7.0 (API 24)");

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            Debug.Log("✅ Scripting Backend: IL2CPP");

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            Debug.Log("✅ Target Architecture: ARM64");

            EditorUserBuildSettings.buildAppBundle = true;
            Debug.Log("✅ Build App Bundle: Enabled");

            PlayerSettings.stripEngineCode = true;
            Debug.Log("✅ Strip Engine Code: Enabled");

            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.High);
            Debug.Log("✅ Managed Stripping: High");

            // Não desligar o keystore: isso fazia o release sair assinado com a chave de debug.
            if (!PlayerSettings.Android.useCustomKeystore)
                Debug.LogWarning("⚠️ Custom Keystore: Not configured (YOU MUST SET THIS MANUALLY)");

            QualitySettings.vSyncCount = 0;
            Debug.Log("✅ VSync: Disabled (better for mobile)");

            Application.targetFrameRate = 60;
            Debug.Log("✅ Target Frame Rate: 60 FPS");

            Debug.Log("\n=== MANUAL STEPS REQUIRED ===");
            Debug.LogWarning("1. Configure Custom Keystore in Player Settings > Publishing Settings");
            Debug.LogWarning("2. Replace test ad unit IDs in AdMobInterstitialProvider.cs");
            Debug.LogWarning("3. Increment bundle version code");
            Debug.LogWarning("4. Test on physical device before building");

            Debug.Log("\n✅ Automated setup complete! Review warnings above.");
        }

        [MenuItem("Tools/Android/Print Current Build Settings")]
        public static void PrintBuildSettings()
        {
            Debug.Log("=== CURRENT ANDROID BUILD SETTINGS ===");
            Debug.Log($"Bundle ID: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)}");
            Debug.Log($"Version: {PlayerSettings.bundleVersion}");
            Debug.Log($"Bundle Version Code: {PlayerSettings.Android.bundleVersionCode}");
            Debug.Log($"Min SDK: {PlayerSettings.Android.minSdkVersion}");
            Debug.Log($"Target SDK: {PlayerSettings.Android.targetSdkVersion}");
            Debug.Log($"Scripting Backend: {PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}");
            Debug.Log($"Target Architecture: {PlayerSettings.Android.targetArchitectures}");
            Debug.Log($"Build App Bundle: {EditorUserBuildSettings.buildAppBundle}");
            Debug.Log($"Custom Keystore: {PlayerSettings.Android.useCustomKeystore}");
            Debug.Log($"IL2CPP Code Generation: {PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.Android)}");
            Debug.Log($"Managed Stripping Level: {PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android)}");
            Debug.Log($"Strip Engine Code: {PlayerSettings.stripEngineCode}");
            Debug.Log("========================================");
        }

        [MenuItem("Tools/Android/Increment Version Code")]
        public static void IncrementVersionCode()
        {
            int currentCode = PlayerSettings.Android.bundleVersionCode;
            PlayerSettings.Android.bundleVersionCode = currentCode + 1;
            
            Debug.Log($"✅ Bundle version code incremented: {currentCode} → {PlayerSettings.Android.bundleVersionCode}");
            Debug.Log("Don't forget to update version name if needed!");
        }

        [MenuItem("Tools/Android/Validate AdMob Setup")]
        public static void ValidateAdMobSetup()
        {
            Debug.Log("=== ADMOB SETUP VALIDATION ===");

            string androidManifestPath = "Assets/Plugins/Android/GoogleMobileAdsPlugin.androidlib/AndroidManifest.xml";
            
            if (System.IO.File.Exists(androidManifestPath))
            {
                string content = System.IO.File.ReadAllText(androidManifestPath);
                
                if (content.Contains("ca-app-pub-8609532543875876"))
                {
                    Debug.Log("✅ Production AdMob App ID found in AndroidManifest.xml");
                }
                else if (content.Contains("ca-app-pub-3940256099942544"))
                {
                    Debug.LogError("❌ TEST AdMob App ID found in AndroidManifest.xml!");
                }
                else
                {
                    Debug.LogWarning("⚠️ Could not verify AdMob App ID in AndroidManifest.xml");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ AndroidManifest not found at: {androidManifestPath}");
            }

            string[] adScripts = new[]
            {
                "Assets/Scripts/Ads/AdMobInterstitialProvider.cs",
                "Assets/Scripts/Ads/AdMobBannerProvider.cs"
            };

            foreach (string scriptPath in adScripts)
            {
                if (System.IO.File.Exists(scriptPath))
                {
                    string content = System.IO.File.ReadAllText(scriptPath);
                    string scriptName = System.IO.Path.GetFileName(scriptPath);
                    
                    if (content.Contains("ca-app-pub-3940256099942544"))
                    {
                        Debug.LogError($"❌ TEST ad unit IDs found in {scriptName}!");
                    }
                    else if (content.Contains("ca-app-pub-8609532543875876"))
                    {
                        Debug.Log($"✅ Production ad unit IDs found in {scriptName}");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Could not verify ad unit IDs in {scriptName}");
                    }
                }
            }

            Debug.Log("====================================");
        }

        [MenuItem("Tools/Android/Open Persistent Data Path")]
        public static void OpenPersistentDataPath()
        {
            string path = Application.persistentDataPath;
            
            if (System.IO.Directory.Exists(path))
            {
                EditorUtility.RevealInFinder(path);
                Debug.Log($"Opened persistent data path: {path}");
            }
            else
            {
                Debug.LogWarning($"Persistent data path does not exist yet: {path}");
            }
        }
    }
}
