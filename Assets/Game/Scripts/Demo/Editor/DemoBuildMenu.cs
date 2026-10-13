using System.IO;
using Obfuz.Settings;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UGF.EditorTools;
using UGF.EditorTools.ResourceTools;
using UnityGameFramework.Editor.ResourceTools;

namespace Shanhai.Demo.Editor
{
    internal static class DemoBuildMenu
    {
        [MenuItem("Shanhai/Demo/Build Windows Player")]
        private static void BuildWindowsPlayer()
        {
            if (ObfuzSettings.Instance != null)
            {
                if (ObfuzSettings.Instance.buildPipelineSettings == null)
                    ObfuzSettings.Instance.buildPipelineSettings = new BuildPipelineSettings();
                ObfuzSettings.Instance.buildPipelineSettings.enable = false;
                ObfuzSettings.Save();
            }
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputRoot = Path.Combine(projectRoot, "BuildApp", "ShanhaiDemoWindows");
            Directory.CreateDirectory(outputRoot);
            if (!BuildPackageResources(outputRoot)) return;
            string executable = Path.Combine(outputRoot, "ShanhaiDemo.exe");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[ShanhaiDemoBuild] 成功: {executable}, size={report.summary.totalSize} bytes");
            }
            else
            {
                Debug.LogError($"[ShanhaiDemoBuild] 失败: {report.summary.result}, errors={report.summary.totalErrors}");
            }
        }

        private static bool BuildPackageResources(string outputRoot)
        {
            const string rulePath = "Assets/Plugins/UnityGameFramework/Configs/ResourceRuleEditor.asset";
            var rules = AssetDatabase.LoadAssetAtPath<ResourceRuleEditorData>(rulePath);
            if (rules == null)
            {
                Debug.LogError("[ShanhaiDemoBuild] 资源规则配置缺失，已停止出包。");
                return false;
            }
            // Empty optional resource categories still need directories for the rule scanner.
            foreach (ResourceRule rule in rules.rules)
            {
                if (rule.valid && !string.IsNullOrEmpty(rule.assetsDirectoryPath) && !Directory.Exists(rule.assetsDirectoryPath))
                    Directory.CreateDirectory(rule.assetsDirectoryPath);
            }
            AssetDatabase.Refresh();
            ResourceRuleEditorUtility.RefreshResourceCollection(rulePath);
            AssetBuildHandler.AutoResolveAbDuplicateAssets();
            var builder = new ResourceBuilderController();
            if (!builder.Load())
            {
                Debug.LogError("[ShanhaiDemoBuild] 资源构建配置加载失败，已停止出包。");
                return false;
            }
            string resourceOutput = Path.Combine(outputRoot, "ResourceBuild");
            Directory.CreateDirectory(resourceOutput);
            builder.OutputDirectory = resourceOutput;
            builder.Platforms = Platform.Windows64;
            builder.OutputPackageSelected = true;
            builder.OutputFullSelected = false;
            builder.OutputPackedSelected = false;
            // The legacy handler discovery excludes Builtin.Editor; copy the package explicitly below.
            builder.BuildEventHandlerTypeName = string.Empty;
            builder.BuildResourceError += message => Debug.LogError("[ShanhaiDemoBuild] 资源构建失败: " + message);
            if (!builder.RefreshCompressionHelper() || !builder.RefreshBuildEventHandler() || !builder.BuildResources())
            {
                Debug.LogError("[ShanhaiDemoBuild] 资源包构建失败，已停止出包。");
                return false;
            }
            string packagePath = Path.Combine(builder.OutputPackagePath, Platform.Windows64.ToString());
            if (!File.Exists(Path.Combine(packagePath, "GameFrameworkVersion.dat")))
            {
                Debug.LogError("[ShanhaiDemoBuild] 生成目录缺少资源包版本文件，已停止出包。");
                return false;
            }
            foreach (string sourceFile in Directory.GetFiles(packagePath, "*", SearchOption.AllDirectories))
            {
                string relativePath = sourceFile.Substring(packagePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string targetFile = Path.Combine(Application.streamingAssetsPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                File.Copy(sourceFile, targetFile, true);
            }
            AssetDatabase.Refresh();
            Debug.Log("[ShanhaiDemoBuild] 资源包及版本文件已生成。");
            return true;
        }
    }
}
