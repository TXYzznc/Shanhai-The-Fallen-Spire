#if UNITY_EDITOR
using GameFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public static class GFDiagnosticRunner
    {
        private const string ReportAction = "gf-diagnostics-run-all";
        private const string SnapshotAction = "gf-diagnostics-snapshot";
        private const string ResourceRuleEditorAsset = "Assets/Plugins/UnityGameFramework/Configs/ResourceRuleEditor.asset";

        [MenuItem("Game Framework/GameTools/Diagnostics/Run All", false, 1021)]
        public static void RunAllFromMenu()
        {
            RunAll();
        }

        public static GFDiagnosticReport RunAll()
        {
            GFTrace.EnableUnityLogCapture();
            if (!Application.isPlaying)
            {
                GFTrace.Clear();
            }
            GFTrace.BeginTrace("diagnostics");
            GFTrace.Info("Diagnostics", "RunAll.Begin");

            var report = new GFDiagnosticReport(ReportAction);
            RunCheck(report, "Project layout", CheckProjectLayout);
            RunCheck(report, "AppConfigs runtime contract", CheckAppConfigs);
            RunCheck(report, "Build settings", CheckBuildSettings);
            RunCheck(report, "Resource rules", CheckResourceRules);
            RunCheck(report, "AI DataTable json", CheckAIDataTableJson);
            RunRegisteredScenarios(report);
            RunCheck(report, "Editor snapshot", CheckSnapshot);

            report.AttachRuntimeContext("diagnostic-run-all");
            report.RefreshSummary();

            string reportFile = GetReportFile(ReportAction);
            GFDiagnosticReport.WriteJson(reportFile, report);
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"GF Diagnostic report: {reportFile}");
            GFTrace.Success("Diagnostics", "RunAll.End", null, GFTrace.Data("reportFile", reportFile, "successCount", report.successCount.ToString(), "failureCount", report.failureCount.ToString(), "warningCount", report.warningCount.ToString()));
            return report;
        }

        [MenuItem("Game Framework/GameTools/Diagnostics/Export Snapshot", false, 1022)]
        public static void ExportSnapshot()
        {
            GFTrace.EnableUnityLogCapture();
            var report = new GFDiagnosticReport(SnapshotAction);
            var item = report.AddItem("Export snapshot");
            item.Pass("Snapshot exported.");
            report.AttachRuntimeContext("manual-export-snapshot");
            report.RefreshSummary();

            string reportFile = GetReportFile(SnapshotAction);
            GFDiagnosticReport.WriteJson(reportFile, report);
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"GF Diagnostic snapshot: {reportFile}");
        }

        [MenuItem("Game Framework/GameTools/Diagnostics/Open Latest Report", false, 1023)]
        public static void OpenLatestReport()
        {
            string reportFile = GetLatestReportFile();
            if (string.IsNullOrWhiteSpace(reportFile))
            {
                UnityEngine.Debug.LogWarning($"No diagnostic report found: {ConstEditor.DiagnosticReportPath}");
                return;
            }

            EditorUtility.RevealInFinder(reportFile);
        }

        private static void RunCheck(GFDiagnosticReport report, string name, Action<GFDiagnosticReportItem> check)
        {
            var item = report.AddItem(name);
            var stopwatch = Stopwatch.StartNew();
            GFTrace.Info("Diagnostics", $"{name}.Begin");
            try
            {
                check(item);
                if (item.errors.Count <= 0)
                {
                    item.success = true;
                }
            }
            catch (Exception exception)
            {
                item.Fail(exception.ToString());
                GFTrace.Exception("Diagnostics", $"{name}.Exception", exception);
            }
            finally
            {
                stopwatch.Stop();
                item.durationMs = stopwatch.ElapsedMilliseconds;
                string result = item.success ? GFTrace.ResultSuccess : GFTrace.ResultFailure;
                GFTrace.Record("Diagnostics", $"{name}.End", result, null, GFTrace.Data("durationMs", item.durationMs.ToString(), "errors", item.errors.Count.ToString(), "warnings", item.warnings.Count.ToString()));
            }
        }

        private static void RunRegisteredScenarios(GFDiagnosticReport report)
        {
            var scenarioTypes = TypeCache.GetTypesDerivedFrom<IGFDiagnosticScenario>()
                .Where(type => type != null && !type.IsAbstract && !type.IsInterface)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            var discoveryItem = report.AddItem("Registered diagnostic scenarios");
            discoveryItem.Detail("discoveredCount", scenarioTypes.Count);
            GFTrace.Info("Diagnostics", "RegisteredScenarios.Discover", null, GFTrace.Data("count", scenarioTypes.Count.ToString()));

            if (scenarioTypes.Count <= 0)
            {
                discoveryItem.Pass("No registered diagnostic scenarios.");
                return;
            }

            int executableCount = 0;
            int skippedCount = 0;
            foreach (Type scenarioType in scenarioTypes)
            {
                IGFDiagnosticScenario scenario = null;
                try
                {
                    scenario = (IGFDiagnosticScenario)Activator.CreateInstance(scenarioType);
                }
                catch (Exception exception)
                {
                    skippedCount++;
                    discoveryItem.Warn($"Scenario can not be created: {scenarioType.FullName}");
                    GFTrace.Exception("Diagnostics", "RegisteredScenarios.Create.Exception", exception, GFTrace.Data("type", scenarioType.FullName));
                    continue;
                }

                if (!CanRunScenario(scenario.Mode))
                {
                    skippedCount++;
                    discoveryItem.Detail($"skipped.{scenarioType.Name}", scenario.Mode);
                    GFTrace.Info("Diagnostics", "RegisteredScenarios.Skip", null, GFTrace.Data("type", scenarioType.FullName, "mode", scenario.Mode.ToString()));
                    continue;
                }

                executableCount++;
                RunScenario(report, scenario, scenarioType);
            }

            discoveryItem.Detail("executableCount", executableCount);
            discoveryItem.Detail("skippedCount", skippedCount);
            discoveryItem.Pass("Registered diagnostic scenario discovery completed.");
        }

        private static void RunScenario(GFDiagnosticReport report, IGFDiagnosticScenario scenario, Type scenarioType)
        {
            string category = string.IsNullOrWhiteSpace(scenario.Category) ? "General" : scenario.Category;
            string name = string.IsNullOrWhiteSpace(scenario.Name) ? scenarioType.Name : scenario.Name;
            var item = report.AddItem($"Scenario/{category}/{name}");
            item.Detail("type", scenarioType.FullName);
            item.Detail("mode", scenario.Mode);

            var stopwatch = Stopwatch.StartNew();
            GFTrace.Info("Diagnostics", "Scenario.Begin", null, GFTrace.Data("type", scenarioType.FullName, "category", category, "name", name, "mode", scenario.Mode.ToString()));
            try
            {
                var context = new GFDiagnosticScenarioContext(item, name, category);
                scenario.Run(context);
                if (item.errors.Count <= 0)
                {
                    item.Pass("Scenario completed.");
                }
            }
            catch (Exception exception)
            {
                item.Fail(exception.ToString());
                GFTrace.Exception("Diagnostics", "Scenario.Exception", exception, GFTrace.Data("type", scenarioType.FullName, "category", category, "name", name));
            }
            finally
            {
                stopwatch.Stop();
                item.durationMs = stopwatch.ElapsedMilliseconds;
                string result = item.success ? GFTrace.ResultSuccess : GFTrace.ResultFailure;
                GFTrace.Record("Diagnostics", "Scenario.End", result, null, GFTrace.Data("type", scenarioType.FullName, "category", category, "name", name, "durationMs", item.durationMs.ToString(), "errors", item.errors.Count.ToString(), "warnings", item.warnings.Count.ToString()));
            }
        }

        private static bool CanRunScenario(GFDiagnosticScenarioMode mode)
        {
            if (mode == GFDiagnosticScenarioMode.Any)
            {
                return true;
            }

            if (mode == GFDiagnosticScenarioMode.PlayMode)
            {
                return Application.isPlaying;
            }

            return !Application.isPlaying;
        }

        private static void CheckProjectLayout(GFDiagnosticReportItem item)
        {
            string[] requiredDirectories =
            {
                "Assets/Game/Scripts",
                "Assets/Game/ScriptsBuiltin",
                "Assets/Plugins/UnityGameFramework",
                "ProjectSettings",
            };

            foreach (string directory in requiredDirectories)
            {
                if (!Directory.Exists(directory))
                {
                    item.Fail($"Required directory does not exist: {directory}");
                }
            }

        }

        private static void CheckAppConfigs(GFDiagnosticReportItem item)
        {
            var appConfig = AppConfigs.ReloadInstanceEditor();
            if (appConfig == null)
            {
                item.Detail("state", "No AppConfigs asset; valid for an empty framework baseline.");
                return;
            }

            item.Detail("loadFromBytes", appConfig.LoadFromBytes);
            CheckStringArrayNotNull(item, "DataTables", appConfig.DataTables);
            CheckStringArrayNotNull(item, "Configs", appConfig.Configs);
            CheckStringArrayNotNull(item, "Languages", appConfig.Languages);
            CheckStringArrayNotNull(item, "Procedures", appConfig.Procedures);

            foreach (string dataTable in appConfig.DataTables ?? Array.Empty<string>())
            {
                string rowTypeName = Path.GetFileName(dataTable.Split('_')[0]);
                if (Utility.Assembly.GetType(rowTypeName) == null)
                {
                    item.Fail($"DataTable row type does not exist: {rowTypeName} ({dataTable})");
                }

                string sourceExcel = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.DataTableExcelPath, $"{dataTable}.xlsx");
                string outputAsset = UtilityBuiltin.AssetsPath.GetDataTablePath(dataTable, appConfig.LoadFromBytes);
                RequireFile(item, sourceExcel);
                RequireAsset(item, outputAsset);
            }

            foreach (string config in appConfig.Configs ?? Array.Empty<string>())
            {
                string sourceExcel = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.ConfigExcelPath, $"{config}.xlsx");
                string outputAsset = UtilityBuiltin.AssetsPath.GetConfigPath(config, appConfig.LoadFromBytes);
                RequireFile(item, sourceExcel);
                RequireAsset(item, outputAsset);
            }

            foreach (string language in appConfig.Languages ?? Array.Empty<string>())
            {
                string sourceExcel = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.LanguageExcelPath, $"{language}.xlsx");
                string outputAsset = UtilityBuiltin.AssetsPath.GetLanguagePath(language, appConfig.LoadFromBytes);
                RequireFile(item, sourceExcel);
                RequireAsset(item, outputAsset);
            }

            foreach (string procedure in appConfig.Procedures ?? Array.Empty<string>())
            {
                Type procedureType = Utility.Assembly.GetType(procedure);
                if (procedureType == null)
                {
                    item.Fail($"Procedure type does not exist: {procedure}");
                }
            }

            item.Detail("dataTables", string.Join(",", appConfig.DataTables ?? Array.Empty<string>()));
            item.Detail("configs", string.Join(",", appConfig.Configs ?? Array.Empty<string>()));
            item.Detail("languages", string.Join(",", appConfig.Languages ?? Array.Empty<string>()));
            item.Detail("procedures", string.Join(",", appConfig.Procedures ?? Array.Empty<string>()));
        }

        private static void CheckBuildSettings(GFDiagnosticReportItem item)
        {
            var scenes = EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>();
            if (scenes.Length <= 0)
            {
                item.Detail("sceneCount", "0");
                item.Detail("state", "Empty framework baseline");
                return;
            }

            item.Detail("sceneCount", scenes.Length);
            foreach (var scene in scenes)
            {
                if (!File.Exists(scene.path))
                {
                    item.Fail($"Build scene does not exist: {scene.path}");
                }

                if (scene.path.Contains("/Examples/") ||
                    scene.path.Contains("\\Examples\\") ||
                    scene.path.Contains("/Samples/") ||
                    scene.path.Contains("\\Samples\\"))
                {
                    item.Fail($"Example or sample scene should not be in build settings: {scene.path}");
                }
            }

            item.Detail("scenes", string.Join(",", scenes.Select(scene => $"{scene.path}|enabled={scene.enabled}")));
        }

        private static void CheckResourceRules(GFDiagnosticReportItem item)
        {
            RequireAsset(item, ResourceRuleEditorAsset);
            if (!File.Exists(ResourceRuleEditorAsset))
            {
                return;
            }

            string text = File.ReadAllText(ResourceRuleEditorAsset);
            if (text.Contains("/Examples/") || text.Contains("\\Examples\\")
                || text.Contains("/Samples/") || text.Contains("\\Samples\\"))
            {
                item.Fail("Resource rules must not include example or sample directories.");
            }
        }

        private static void CheckAIDataTableJson(GFDiagnosticReportItem item)
        {
            if (!Directory.Exists(ConstEditor.AIDataTablePath))
            {
                item.Detail("state", "No data-table input directory; valid for an empty framework baseline.");
                return;
            }

            var jsonFiles = Directory.GetFiles(ConstEditor.AIDataTablePath, "*.json", SearchOption.AllDirectories)
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .ToList();
            item.Detail("jsonCount", jsonFiles.Count);
            if (jsonFiles.Count <= 0)
            {
                item.Detail("state", "No data-table input files; valid for an empty framework baseline.");
                return;
            }

            var aiReport = AIGameDataTableGenerator.ImportDataTablesFromAIJson(jsonFiles, syncExcel: false, writeGeneratedFiles: false);
            aiReport.RefreshSummary();
            item.Detail("aiSuccessCount", aiReport.successCount);
            item.Detail("aiFailureCount", aiReport.failureCount);
            item.Detail("aiWarningCount", aiReport.warningCount);

            foreach (string warning in aiReport.warnings)
            {
                item.Warn(warning);
            }

            foreach (var aiItem in aiReport.items)
            {
                foreach (string warning in aiItem.warnings)
                {
                    item.Warn($"{aiItem.tableName}: {warning}");
                }

                foreach (string error in aiItem.errors)
                {
                    item.Fail($"{aiItem.tableName}: {error}");
                }
            }

            if (aiReport.failureCount > 0)
            {
                item.Fail($"AI DataTable validation failed: {aiReport.failureCount}");
            }

        }

        private static void CheckSnapshot(GFDiagnosticReportItem item)
        {
            var snapshot = GFDiagnosticSnapshot.Capture("editor-check", 80);
            item.Detail("isPlaying", snapshot.isPlaying);
            item.Detail("loadedScenes", snapshot.loadedScenes.Count);
            item.Detail("currentProcedure", snapshot.currentProcedure);
            item.Detail("snapshotWarnings", snapshot.warnings.Count);
            foreach (string warning in snapshot.warnings)
            {
                item.Warn(warning);
            }
        }

        private static void CheckStringArrayNotNull(GFDiagnosticReportItem item, string name, string[] value)
        {
            if (value == null)
            {
                item.Fail($"AppConfigs.{name} is null.");
            }
        }

        private static void RequireAsset(GFDiagnosticReportItem item, string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                item.Fail($"Asset does not exist: {assetPath}");
            }
        }

        private static void RequireFile(GFDiagnosticReportItem item, string fileName)
        {
            if (!File.Exists(fileName))
            {
                item.Fail($"File does not exist: {fileName}");
            }
        }

        private static string GetReportFile(string action)
        {
            return UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.DiagnosticReportPath, $"{action}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        }

        private static string GetLatestReportFile()
        {
            if (!Directory.Exists(ConstEditor.DiagnosticReportPath))
            {
                return null;
            }

            return Directory.GetFiles(ConstEditor.DiagnosticReportPath, "*.json", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }
    }
}
#endif
