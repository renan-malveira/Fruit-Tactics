using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Gera o projeto Xcode do iOS em Builds/iOS (fora de Assets), sem Development Build.
    ///
    /// Menu: Tools/Release/4. Gerar projeto Xcode - iOS
    /// Terminal (com o Unity fechado):
    ///   Unity -batchmode -quit -projectPath "Tatics Fruits" -buildTarget iOS \
    ///         -executeMethod Editor.IosBuildHelper.BuildFromCommandLine -logFile -
    /// </summary>
    public static class IosBuildHelper
    {
        private const string OutputPath = "Builds/iOS";

        [MenuItem("Tools/Release/4. Gerar projeto Xcode - iOS")]
        public static void BuildFromMenu()
        {
            var result = Build();
            EditorUtility.DisplayDialog("Build iOS",
                result == BuildResult.Succeeded
                    ? $"Projeto Xcode gerado em {OutputPath}.\nAbra Unity-iPhone.xcworkspace."
                    : $"Build terminou com resultado {result}. Veja o Console.", "OK");
        }

        public static void BuildFromCommandLine()
        {
            var result = Build();
            EditorApplication.Exit(result == BuildResult.Succeeded ? 0 : 1);
        }

        private static BuildResult Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("Nenhuma cena habilitada em Build Settings.");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[IosBuildHelper] {summary.result}: {summary.totalErrors} erro(s), " +
                      $"{summary.totalWarnings} aviso(s), {summary.totalTime}, saída {summary.outputPath}");
            return summary.result;
        }

        /// <summary>
        /// O Xcode 26+ recusa pods com deployment target abaixo do iOS 15 (Firebase e AdMob
        /// declaram 9 a 12) e o Archive falha. Alinha todos os pods ao mínimo do app.
        /// Ordem 45: depois do Podfile gerado pelo iOS Resolver (40) e antes do pod install (50).
        /// </summary>
        [PostProcessBuild(45)]
        public static void FixPodsDeploymentTarget(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS) return;

            var podfile = Path.Combine(buildPath, "Podfile");
            if (!File.Exists(podfile)) return;

            var text = File.ReadAllText(podfile);
            if (text.Contains("post_install")) return;

            var minIos = PlayerSettings.iOS.targetOSVersionString;
            File.AppendAllText(podfile,
                "\npost_install do |installer|\n" +
                "  installer.pods_project.targets.each do |t|\n" +
                "    t.build_configurations.each do |config|\n" +
                $"      config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] = '{minIos}'\n" +
                "    end\n" +
                "  end\n" +
                "end\n");
            Debug.Log($"[IosBuildHelper] Podfile: deployment target dos pods = iOS {minIos}");
        }
    }
}
