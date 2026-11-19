using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class ProductionReadinessValidator
    {
        private const string GOOGLE_TEST_AD_PREFIX = "ca-app-pub-3940256099942544";

        [MenuItem("Tools/Validate Production Readiness")]
        public static void ValidateProductionReadiness()
        {
            bool isReady = true;
            int issueCount = 0;

            Debug.Log("=== PRODUCTION READINESS CHECK ===");

            if (CheckTestAdIds())
            {
                Debug.LogError("❌ TEST AD IDS DETECTED! Replace test ad unit IDs in Core/StoreConfig.cs with production IDs before release.");
                isReady = false;
                issueCount++;
            }
            else
            {
                Debug.Log("✅ No test ad IDs detected");
            }

            if (PlayerSettings.Android.bundleVersionCode < 1)
            {
                Debug.LogWarning("⚠️ Bundle version code is less than 1");
                issueCount++;
            }
            else
            {
                Debug.Log($"✅ Bundle version code: {PlayerSettings.Android.bundleVersionCode}");
            }

            if (string.IsNullOrEmpty(PlayerSettings.companyName))
            {
                Debug.LogWarning("⚠️ Company name is not set");
                issueCount++;
            }
            else
            {
                Debug.Log($"✅ Company name: {PlayerSettings.companyName}");
            }

            if (string.IsNullOrEmpty(PlayerSettings.productName))
            {
                Debug.LogError("❌ Product name is not set");
                isReady = false;
                issueCount++;
            }
            else
            {
                Debug.Log($"✅ Product name: {PlayerSettings.productName}");
            }

            string bundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            if (string.IsNullOrEmpty(bundleId) || bundleId.Contains("DefaultCompany"))
            {
                Debug.LogError("❌ Invalid or default bundle identifier detected");
                isReady = false;
                issueCount++;
            }
            else
            {
                Debug.Log($"✅ Bundle ID: {bundleId}");
            }

            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel25)
            {
                Debug.LogWarning($"⚠️ Min SDK version is {PlayerSettings.Android.minSdkVersion}. Use Android 7.1 (API 25) or higher for Google Play");
            }
            else
            {
                Debug.Log($"✅ Min SDK version: {PlayerSettings.Android.minSdkVersion}");
            }

            if (!PlayerSettings.Android.useCustomKeystore)
            {
                Debug.LogWarning("⚠️ Custom keystore not configured. Required for Google Play release!");
                issueCount++;
            }
            else
            {
                Debug.Log("✅ Custom keystore configured");
            }

            Debug.Log("=== SCENES IN BUILD ===");
            if (EditorBuildSettings.scenes.Length == 0)
            {
                Debug.LogError("❌ No scenes added to build settings!");
                isReady = false;
                issueCount++;
            }
            else
            {
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    if (scene.enabled)
                        Debug.Log($"  ✅ {scene.path}");
                }
            }

            Debug.Log("================================");

            if (isReady && issueCount == 0)
            {
                Debug.Log("✅✅✅ PROJECT IS READY FOR PRODUCTION! ✅✅✅");
            }
            else if (isReady)
            {
                Debug.LogWarning($"⚠️ Project is buildable but has {issueCount} warnings. Review recommended before release.");
            }
            else
            {
                Debug.LogError($"❌ PROJECT NOT READY! Found {issueCount} issues. Fix critical errors before release.");
            }
        }

        private static bool CheckTestAdIds()
        {
            var guids = new System.Collections.Generic.List<string>();
            guids.AddRange(AssetDatabase.FindAssets("StoreConfig t:Script"));
            guids.AddRange(AssetDatabase.FindAssets("AdMobInterstitialProvider t:Script"));
            guids.AddRange(AssetDatabase.FindAssets("AdMobBannerProvider t:Script"));
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string content = System.IO.File.ReadAllText(path);
                
                if (content.Contains(GOOGLE_TEST_AD_PREFIX))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
