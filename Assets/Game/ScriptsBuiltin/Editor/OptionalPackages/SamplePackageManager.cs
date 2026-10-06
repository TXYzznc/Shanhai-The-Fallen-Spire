using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AiFriendlyFrame.Editor.Samples
{
    [Serializable]
    internal sealed class SamplePackageManifest
    {
        public string id;
        public string displayName;
        public string version;
        public string entryScene;
        public string installRoot;
        public SamplePayloadMapping[] payloads;
        public SampleAppConfigsRegistration appConfigs;
        public SampleAppConfigsProfile appConfigsProfile;
        public bool addEntrySceneToBuildSettings;
    }

    [Serializable]
    internal sealed class SamplePayloadMapping
    {
        public string source;
        public string destination;
    }

    [Serializable]
    internal sealed class SampleAppConfigsRegistration
    {
        public string[] dataTables;
        public string[] configs;
        public string[] languages;
        public string[] procedures;
    }

    [Serializable]
    internal sealed class SampleAppConfigsProfile
    {
        public bool loadFromBytes;
        public string[] dataTables;
        public string[] configs;
        public string[] languages;
        public string[] procedures;
    }

    [Serializable]
    internal sealed class SampleAppConfigsSnapshot
    {
        public bool loadFromBytes;
        public string[] dataTables;
        public string[] configs;
        public string[] languages;
        public string[] procedures;
        public string sha256;
    }

    [Serializable]
    internal sealed class SampleAppConfigsProfileBackup
    {
        public string stateDirectory;
        public string backupAssetPath;
        public string assetPath;
        public string backupSha256;
        public SampleAppConfigsSnapshot before;
        public SampleAppConfigsSnapshot applied;
        public string phase;
    }

    [Serializable]
    internal sealed class SampleBuildSettingsSnapshot
    {
        public SampleBuildSettingsScene[] scenes;
        public string sha256;
    }

    [Serializable]
    internal sealed class SampleBuildSettingsScene
    {
        public string path;
        public bool enabled;
    }

    [Serializable]
    internal sealed class SampleInstallRecord
    {
        public string id;
        public string version;
        public string entryScene;
        public SampleInstalledFile[] files;
        public SampleAppConfigsSnapshot appConfigsBefore;
        public SampleAppConfigsSnapshot appConfigsAfter;
        public SampleAppConfigsProfile appConfigsProfile;
        public SampleAppConfigsProfileBackup appConfigsProfileBackup;
        public SampleBuildSettingsSnapshot buildSettingsBefore;
        public SampleBuildSettingsSnapshot buildSettingsAfter;
    }

    [Serializable]
    internal sealed class SampleInstalledFile
    {
        public string path;
        public string sha256;
    }

    internal readonly struct SamplePackageInfo
    {
        public SamplePackageInfo(string packageDirectory, SamplePackageManifest manifest, string error)
        {
            PackageDirectory = packageDirectory;
            Manifest = manifest;
            Error = error;
        }

        public string PackageDirectory { get; }
        public SamplePackageManifest Manifest { get; }
        public string Error { get; }
        public bool IsValid => Manifest != null && string.IsNullOrEmpty(Error);
    }

    internal static class SamplePackageManager
    {
        internal const string SamplesRootRelativePath = "Samples~";
        internal const string SampleInstallRootRelativePath = "Assets/Sample";

        private const string ManifestFileName = "sample.json";
        private const string InstallRecordFileName = ".sample-install.json";
        private const string SampleStateRootRelativePath = ".ai-friendly-frame/sample-state";
        private const string AppConfigsBackupFileName = "AppConfigs.asset.backup";
        private const string AppConfigsProfileStateFileName = "profile-state.json";

        internal static IReadOnlyList<SamplePackageInfo> DiscoverPackages()
        {
            var packages = new List<SamplePackageInfo>();
            string root = ResolveProjectPath(SamplesRootRelativePath);
            if (!Directory.Exists(root))
            {
                return packages;
            }

            foreach (string directory in Directory.GetDirectories(root))
            {
                packages.Add(LoadPackage(directory));
            }

            packages.Sort((left, right) => string.Compare(left.Manifest?.displayName ?? left.PackageDirectory,
                right.Manifest?.displayName ?? right.PackageDirectory, StringComparison.OrdinalIgnoreCase));
            return packages;
        }

        internal static bool IsInstalled(SamplePackageInfo package)
        {
            return package.IsValid && File.Exists(GetInstallRecordPath(package.Manifest));
        }

        internal static bool TryInstall(SamplePackageInfo package, out string message)
        {
            if (!EnsureEditorReadyForPackageMutation(out message))
            {
                return false;
            }

            if (!package.IsValid)
            {
                message = package.Error ?? "示例包无效。";
                return false;
            }

            if (IsInstalled(package))
            {
                message = $"{package.Manifest.displayName} 已安装。";
                return false;
            }

            if (!ValidateManifest(package.PackageDirectory, package.Manifest, out message))
            {
                return false;
            }

            var copiedFiles = new List<SampleInstalledFile>();
            SampleAppConfigsSnapshot appConfigsBefore = null;
            SampleAppConfigsSnapshot appConfigsAfter = null;
            SampleAppConfigsProfileBackup appConfigsProfileBackup = null;
            SampleBuildSettingsSnapshot buildSettingsBefore = null;
            SampleBuildSettingsSnapshot buildSettingsAfter = null;
            try
            {
                foreach (SamplePayloadMapping payload in package.Manifest.payloads)
                {
                    string sourceRoot = ResolvePackagePath(package.PackageDirectory, payload.source);
                    string destinationRoot = ResolveProjectPath(payload.destination);
                    foreach (string sourceFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
                    {
                        string relativeFile = GetRelativePath(sourceRoot, sourceFile);
                        string destinationFile = Path.Combine(destinationRoot, relativeFile);
                        string destinationRelativePath = ToProjectRelativePath(destinationFile);

                        if (File.Exists(destinationFile))
                        {
                            throw new IOException($"安装冲突：'{destinationRelativePath}' 已存在。新包不会占用已有文件。");
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile) ?? destinationRoot);
                        File.Copy(sourceFile, destinationFile, false);

                        copiedFiles.Add(new SampleInstalledFile
                        {
                            path = destinationRelativePath,
                            sha256 = ComputeSha256(destinationFile),
                        });
                    }
                }

                if (!TryApplyAppConfigs(package.Manifest, out appConfigsBefore, out appConfigsAfter,
                        out appConfigsProfileBackup, out message))
                {
                    throw new InvalidOperationException(message);
                }

                if (!TryApplyBuildSettings(package.Manifest, out buildSettingsBefore, out buildSettingsAfter, out message))
                {
                    throw new InvalidOperationException(message);
                }

                // Persist a recoverable record before importing copied C# assets. Asset refresh can trigger a
                // domain reload, and the record preserves the AppConfigs and Build Settings rollback snapshots.
                WriteInstallRecord(package.Manifest, copiedFiles, appConfigsBefore, appConfigsAfter, appConfigsProfileBackup,
                    buildSettingsBefore, buildSettingsAfter);

                // Importers or project hooks may normalize text encoding during this step, so refresh the
                // hashes and replace the provisional record once the import completes.
                AssetDatabase.Refresh();
                RefreshInstalledFileHashes(copiedFiles);
                WriteInstallRecord(package.Manifest, copiedFiles, appConfigsBefore, appConfigsAfter, appConfigsProfileBackup,
                    buildSettingsBefore, buildSettingsAfter);
                AssetDatabase.Refresh();
                message = $"已安装 {package.Manifest.displayName} {package.Manifest.version}。";
                return true;
            }
            catch (Exception exception)
            {
                DeleteInstallRecord(package.Manifest);
                if (buildSettingsBefore != null)
                {
                    RestoreBuildSettings(buildSettingsBefore);
                }

                if (appConfigsProfileBackup != null)
                {
                    RestoreAppConfigsProfile(appConfigsProfileBackup);
                    DeleteAppConfigsProfileState(appConfigsProfileBackup);
                }
                else if (appConfigsBefore != null)
                {
                    RestoreAppConfigs(appConfigsBefore);
                }
                DeleteCopiedFiles(copiedFiles);
                AssetDatabase.Refresh();
                message = $"安装失败：{exception.Message}";
                return false;
            }
        }

        internal static bool TryValidate(SamplePackageInfo package, out string message)
        {
            if (!package.IsValid)
            {
                message = package.Error ?? "示例包无效。";
                return false;
            }

            SampleInstallRecord record = ReadInstallRecord(package.Manifest);
            if (record == null)
            {
                message = "示例包未安装。";
                return false;
            }

            if (!string.Equals(record.id, package.Manifest.id, StringComparison.Ordinal))
            {
                message = "安装记录不属于当前选择的示例包。";
                return false;
            }

            if (!string.Equals(record.id, package.Manifest.id, StringComparison.Ordinal) ||
                !string.Equals(record.version, package.Manifest.version, StringComparison.Ordinal))
            {
                message = "安装记录与当前示例包版本不匹配。";
                return false;
            }

            if (record.files == null || record.files.Length == 0)
            {
                message = "安装记录中没有文件。";
                return false;
            }

            foreach (SampleInstalledFile file in record.files)
            {
                if (file == null || string.IsNullOrWhiteSpace(file.path) || string.IsNullOrWhiteSpace(file.sha256))
                {
                message = "安装记录包含无效文件条目。";
                    return false;
                }

                string fullPath;
                try
                {
                    if (!IsDeclaredDestinationPath(package.Manifest, file.path))
                    {
                message = $"安装记录引用了包载荷范围外的路径：{file.path}";
                        return false;
                    }

                    fullPath = ResolveProjectPath(file.path);
                }
                catch (Exception exception)
                {
                message = $"安装记录包含无效路径：{exception.Message}";
                    return false;
                }

                if (!File.Exists(fullPath))
                {
                message = $"已安装文件缺失：{file.path}";
                    return false;
                }

                if (!string.Equals(file.sha256, ComputeSha256(fullPath), StringComparison.OrdinalIgnoreCase))
                {
                message = $"已安装文件已被修改：{file.path}";
                    return false;
                }
            }

            if (!TryValidateAppConfigs(record, out message))
            {
                return false;
            }

            if (!TryValidateBuildSettings(record, out message))
            {
                return false;
            }

            message = $"{package.Manifest.displayName} 已安装且校验通过。";
            return true;
        }

        internal static bool TryUninstall(SamplePackageInfo package, bool allowModifiedFiles, out string message)
        {
            if (!EnsureEditorReadyForPackageMutation(out message))
            {
                return false;
            }

            if (!package.IsValid)
            {
                message = package.Error ?? "示例包无效。";
                return false;
            }

            SampleInstallRecord record = ReadInstallRecord(package.Manifest);
            if (record == null)
            {
                message = "示例包未安装。";
                return false;
            }

            if (!string.Equals(record.id, package.Manifest.id, StringComparison.Ordinal))
            {
                message = "安装记录不属于当前选择的示例包。";
                return false;
            }

            if (!allowModifiedFiles && !TryValidate(package, out message))
            {
                return false;
            }

            if (!TryValidateAppConfigs(record, out message))
            {
                return false;
            }

            if (!TryValidateBuildSettings(record, out message))
            {
                return false;
            }

            try
            {
                if (record.buildSettingsBefore != null)
                {
                    RestoreBuildSettings(record.buildSettingsBefore);
                }

                if (record.appConfigsProfileBackup != null)
                {
                    RestoreAppConfigsProfile(record.appConfigsProfileBackup);
                }
                else if (record.appConfigsBefore != null)
                {
                    RestoreAppConfigs(record.appConfigsBefore);
                }

                if (record.files != null)
                {
                    foreach (SampleInstalledFile file in record.files)
                    {
                        if (file == null || string.IsNullOrWhiteSpace(file.path))
                        {
                            continue;
                        }

                        if (!IsDeclaredDestinationPath(package.Manifest, file.path))
                        {
                            throw new InvalidDataException($"安装记录引用了包载荷范围外的路径：{file.path}");
                        }

                        string fullPath = ResolveProjectPath(file.path);
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                        }
                    }
                }

                string installRecordPath = GetInstallRecordPath(package.Manifest);
                if (File.Exists(installRecordPath))
                {
                    File.Delete(installRecordPath);
                }

                if (record.appConfigsProfileBackup != null)
                {
                    DeleteAppConfigsProfileState(record.appConfigsProfileBackup);
                }

                DeleteEmptyDirectories(ResolveProjectPath(package.Manifest.installRoot));
                foreach (SamplePayloadMapping payload in package.Manifest.payloads)
                {
                    DeleteEmptyDirectories(ResolveProjectPath(payload.destination));
                    DeleteEmptySampleParentDirectory(payload.destination);
                }
                AssetDatabase.Refresh();
                message = $"已移除 {package.Manifest.displayName}。";
                return true;
            }
            catch (Exception exception)
            {
                AssetDatabase.Refresh();
                message = $"移除失败：{exception.Message}";
                return false;
            }
        }

        internal static bool TryRepair(SamplePackageInfo package, out string message)
        {
            if (!TryUninstall(package, true, out message))
            {
                return false;
            }

            return TryInstall(package, out message);
        }

        internal static bool TryOpenEntryScene(SamplePackageInfo package, out string message)
        {
            if (!IsInstalled(package))
            {
                message = "请先安装示例包，再打开其场景。";
                return false;
            }

            string entryScenePath;
            try
            {
                entryScenePath = ResolveProjectPath(package.Manifest.entryScene);
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return false;
            }

            if (!File.Exists(entryScenePath))
            {
                message = $"入口场景不存在：{package.Manifest.entryScene}";
                return false;
            }

            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                message = "当前场景尚未保存，已取消打开操作。";
                return false;
            }

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(package.Manifest.entryScene);
                message = $"已打开 {package.Manifest.entryScene}。";
            return true;
        }

        internal static void RevealPackage(SamplePackageInfo package)
        {
            if (package.IsValid)
            {
                EditorUtility.RevealInFinder(package.PackageDirectory);
            }
        }

        internal static void RevealInstalledFiles(SamplePackageInfo package)
        {
            if (package.IsValid)
            {
                EditorUtility.RevealInFinder(ResolveProjectPath(package.Manifest.installRoot));
            }
        }

        private static SamplePackageInfo LoadPackage(string packageDirectory)
        {
            string manifestPath = Path.Combine(packageDirectory, ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                return new SamplePackageInfo(packageDirectory, null, "缺少 sample.json。");
            }

            try
            {
                string manifestJson = File.ReadAllText(manifestPath);
                var manifest = JsonUtility.FromJson<SamplePackageManifest>(manifestJson);
                NormalizeOptionalManifestSections(manifestJson, manifest);
                return ValidateManifest(packageDirectory, manifest, out string error)
                    ? new SamplePackageInfo(packageDirectory, manifest, null)
                    : new SamplePackageInfo(packageDirectory, manifest, error);
            }
            catch (Exception exception)
            {
                return new SamplePackageInfo(packageDirectory, null, $"无法读取 sample.json：{exception.Message}");
            }
        }

        private static void NormalizeOptionalManifestSections(string manifestJson, SamplePackageManifest manifest)
        {
            if (manifest == null)
            {
                return;
            }

            // JsonUtility may materialize an empty nested serializable object for a missing field.
            // Presence must therefore be determined from the manifest text, not only its deserialized value.
            if (!HasNonNullJsonProperty(manifestJson, "appConfigs") || IsEmptyAppConfigsRegistration(manifest.appConfigs))
            {
                manifest.appConfigs = null;
            }

            if (!HasNonNullJsonProperty(manifestJson, "appConfigsProfile"))
            {
                manifest.appConfigsProfile = null;
            }
        }

        private static bool HasNonNullJsonProperty(string json, string propertyName)
        {
            return Regex.IsMatch(json,
                "\\\"" + Regex.Escape(propertyName) + "\\\"\\s*:\\s*(?!null\\b)",
                RegexOptions.CultureInvariant);
        }

        private static bool ValidateManifest(string packageDirectory, SamplePackageManifest manifest, out string message)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.id) || string.IsNullOrWhiteSpace(manifest.displayName) ||
                string.IsNullOrWhiteSpace(manifest.version) || string.IsNullOrWhiteSpace(manifest.entryScene) ||
                string.IsNullOrWhiteSpace(manifest.installRoot) || manifest.payloads == null || manifest.payloads.Length == 0)
            {
                message = "清单必须包含 id、displayName、version、entryScene、installRoot 和 payloads。";
                return false;
            }

            if (!manifest.installRoot.StartsWith(SampleInstallRootRelativePath + "/", StringComparison.Ordinal))
            {
                message = $"installRoot 必须位于 {SampleInstallRootRelativePath} 内。";
                return false;
            }

            if (manifest.addEntrySceneToBuildSettings &&
                (!manifest.entryScene.StartsWith("Assets/", StringComparison.Ordinal) ||
                 !manifest.entryScene.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)))
            {
                message = "需要加入 Build Settings 的入口场景必须是 Assets/ 下的 .unity 场景。";
                return false;
            }

            try
            {
                ResolveProjectPath(manifest.entryScene);
                ResolveProjectPath(manifest.installRoot);
                foreach (SamplePayloadMapping payload in manifest.payloads)
                {
                    if (payload == null || string.IsNullOrWhiteSpace(payload.source) || string.IsNullOrWhiteSpace(payload.destination))
                    {
                message = "每个 payload 都必须包含 source 和 destination。";
                        return false;
                    }

                    if (!IsAllowedPayloadDestination(payload.destination))
                    {
                        message = $"payload 的 destination 必须位于 Assets/ 或 GameData/*/Sample/：{payload.destination}";
                        return false;
                    }

                    string sourcePath = ResolvePackagePath(packageDirectory, payload.source);
                    if (!Directory.Exists(sourcePath))
                    {
                message = $"payload 源目录不存在：{payload.source}";
                        return false;
                    }

                    ResolveProjectPath(payload.destination);
                }

                if (manifest.appConfigs != null && manifest.appConfigsProfile != null)
                {
                    message = "示例包不能同时声明增量 AppConfigs 登记和完整 AppConfigs 配置档。";
                    return false;
                }

                if (!ValidateAppConfigsRegistration(manifest.appConfigs, out message) ||
                    !ValidateAppConfigsProfile(manifest.appConfigsProfile, out message))
                {
                    return false;
                }

                if (!ValidateSampleId(manifest.id))
                {
                    message = "示例包 id 只能包含小写字母、数字和连字符。";
                    return false;
                }
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return false;
            }

            message = null;
            return true;
        }

        private static void WriteInstallRecord(SamplePackageManifest manifest, List<SampleInstalledFile> files,
            SampleAppConfigsSnapshot appConfigsBefore, SampleAppConfigsSnapshot appConfigsAfter,
            SampleAppConfigsProfileBackup appConfigsProfileBackup, SampleBuildSettingsSnapshot buildSettingsBefore,
            SampleBuildSettingsSnapshot buildSettingsAfter)
        {
            string recordPath = GetInstallRecordPath(manifest);
            Directory.CreateDirectory(Path.GetDirectoryName(recordPath) ?? ResolveProjectPath(manifest.installRoot));
            var record = new SampleInstallRecord
            {
                id = manifest.id,
                version = manifest.version,
                entryScene = manifest.entryScene,
                files = files.ToArray(),
                appConfigsBefore = appConfigsBefore,
                appConfigsAfter = appConfigsAfter,
                appConfigsProfile = manifest.appConfigsProfile,
                appConfigsProfileBackup = appConfigsProfileBackup,
                buildSettingsBefore = buildSettingsBefore,
                buildSettingsAfter = buildSettingsAfter,
            };
            File.WriteAllText(recordPath, JsonUtility.ToJson(record, true));
        }

        private static SampleInstallRecord ReadInstallRecord(SamplePackageManifest manifest)
        {
            string recordPath = GetInstallRecordPath(manifest);
            if (!File.Exists(recordPath))
            {
                return null;
            }

            try
            {
                string recordJson = File.ReadAllText(recordPath);
                SampleInstallRecord record = JsonUtility.FromJson<SampleInstallRecord>(recordJson);
                NormalizeOptionalInstallRecordSections(recordJson, record);
                return record;
            }
            catch
            {
                return null;
            }
        }

        private static void NormalizeOptionalInstallRecordSections(string recordJson, SampleInstallRecord record)
        {
            if (record == null)
            {
                return;
            }

            if (!HasNonNullJsonProperty(recordJson, "appConfigsBefore") || !HasSnapshotHash(record.appConfigsBefore))
            {
                record.appConfigsBefore = null;
            }

            if (!HasNonNullJsonProperty(recordJson, "appConfigsAfter") || !HasSnapshotHash(record.appConfigsAfter))
            {
                record.appConfigsAfter = null;
            }

            if (!HasNonNullJsonProperty(recordJson, "appConfigsProfile") ||
                !HasUsableAppConfigsProfileBackup(record.appConfigsProfileBackup))
            {
                record.appConfigsProfile = null;
            }

            if (!HasNonNullJsonProperty(recordJson, "appConfigsProfileBackup") ||
                !HasUsableAppConfigsProfileBackup(record.appConfigsProfileBackup))
            {
                record.appConfigsProfileBackup = null;
            }

            if (!HasNonNullJsonProperty(recordJson, "buildSettingsBefore") || !HasBuildSettingsHash(record.buildSettingsBefore))
            {
                record.buildSettingsBefore = null;
            }

            if (!HasNonNullJsonProperty(recordJson, "buildSettingsAfter") || !HasBuildSettingsHash(record.buildSettingsAfter))
            {
                record.buildSettingsAfter = null;
            }
        }

        private static bool IsEmptyAppConfigsRegistration(SampleAppConfigsRegistration registration)
        {
            return registration == null ||
                   IsNullOrEmpty(registration.dataTables) &&
                   IsNullOrEmpty(registration.configs) &&
                   IsNullOrEmpty(registration.languages) &&
                   IsNullOrEmpty(registration.procedures);
        }

        private static bool IsNullOrEmpty(string[] entries)
        {
            return entries == null || entries.Length == 0;
        }

        private static bool HasSnapshotHash(SampleAppConfigsSnapshot snapshot)
        {
            return snapshot != null && !string.IsNullOrWhiteSpace(snapshot.sha256);
        }

        private static bool HasUsableAppConfigsProfileBackup(SampleAppConfigsProfileBackup backup)
        {
            return backup != null && !string.IsNullOrWhiteSpace(backup.assetPath) &&
                   !string.IsNullOrWhiteSpace(backup.backupAssetPath) &&
                   !string.IsNullOrWhiteSpace(backup.backupSha256) && HasSnapshotHash(backup.before) &&
                   HasSnapshotHash(backup.applied);
        }

        private static bool HasBuildSettingsHash(SampleBuildSettingsSnapshot snapshot)
        {
            return snapshot != null && !string.IsNullOrWhiteSpace(snapshot.sha256) && snapshot.scenes != null;
        }

        private static void DeleteCopiedFiles(List<SampleInstalledFile> copiedFiles)
        {
            for (int index = copiedFiles.Count - 1; index >= 0; index--)
            {
                string fullPath = ResolveProjectPath(copiedFiles[index].path);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }

        private static void RefreshInstalledFileHashes(List<SampleInstalledFile> files)
        {
            foreach (SampleInstalledFile file in files)
            {
                string fullPath = ResolveProjectPath(file.path);
                if (!File.Exists(fullPath))
                {
                    throw new IOException($"导入后找不到已安装文件：{file.path}");
                }

                file.sha256 = ComputeSha256(fullPath);
            }
        }

        private static void DeleteEmptyDirectories(string root)
        {
            if (!Directory.Exists(root))
            {
                return;
            }

            string[] directories = Directory.GetDirectories(root, "*", SearchOption.AllDirectories);
            Array.Sort(directories, (left, right) => right.Length.CompareTo(left.Length));
            foreach (string directory in directories)
            {
                if (!Directory.EnumerateFileSystemEntries(directory).GetEnumerator().MoveNext())
                {
                    Directory.Delete(directory, false);
                    DeleteFolderMetaFile(directory);
                }
            }

            if (!Directory.EnumerateFileSystemEntries(root).GetEnumerator().MoveNext())
            {
                Directory.Delete(root, false);
                DeleteFolderMetaFile(root);
            }
        }

        private static void DeleteEmptySampleParentDirectory(string destination)
        {
            string normalizedDestination = destination.Replace('\\', '/').TrimEnd('/');
            int sampleDirectoryIndex = normalizedDestination.LastIndexOf("/Sample/", StringComparison.Ordinal);
            if (sampleDirectoryIndex < 0)
            {
                return;
            }

            string sampleRoot = normalizedDestination.Substring(0, sampleDirectoryIndex + "/Sample".Length);
            DeleteEmptyDirectories(ResolveProjectPath(sampleRoot));
        }

        private static void DeleteFolderMetaFile(string directory)
        {
            string metaPath = directory + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }

        private static bool IsAllowedPayloadDestination(string destination)
        {
            return destination.StartsWith("Assets/", StringComparison.Ordinal) ||
                   IsSampleGameDataDestination(destination, "GameData/DataTables/Sample") ||
                   IsSampleGameDataDestination(destination, "GameData/Configs/Sample") ||
                   IsSampleGameDataDestination(destination, "GameData/Languages/Sample");
        }

        private static bool IsSampleGameDataDestination(string destination, string sampleRoot)
        {
            return string.Equals(destination, sampleRoot, StringComparison.Ordinal) ||
                   destination.StartsWith(sampleRoot + "/", StringComparison.Ordinal);
        }

        private static bool ValidateAppConfigsRegistration(SampleAppConfigsRegistration registration, out string message)
        {
            if (registration == null)
            {
                message = null;
                return true;
            }

            if (!ValidateSampleEntries(registration.dataTables, "DataTable", "Sample/") ||
                !ValidateSampleEntries(registration.configs, "Config", "Sample/") ||
                !ValidateSampleEntries(registration.languages, "Language", "Sample/") ||
                !ValidateSampleEntries(registration.procedures, "Procedure", "AiFriendlyFrame.Sample."))
            {
                message = "AppConfigs 登记项必须属于 Sample 命名空间。";
                return false;
            }

            message = null;
            return true;
        }

        private static bool ValidateAppConfigsProfile(SampleAppConfigsProfile profile, out string message)
        {
            if (profile == null)
            {
                message = null;
                return true;
            }

            if (profile.dataTables == null || profile.configs == null || profile.languages == null || profile.procedures == null)
            {
                message = "完整 AppConfigs 配置档必须声明 dataTables、configs、languages 和 procedures。";
                return false;
            }

            if (!ValidateEntries(profile.dataTables) || !ValidateEntries(profile.configs) ||
                !ValidateEntries(profile.languages) || !ValidateEntries(profile.procedures))
            {
                message = "完整 AppConfigs 配置档不能包含空项。";
                return false;
            }

            message = null;
            return true;
        }

        private static bool ValidateEntries(string[] entries)
        {
            foreach (string entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateSampleId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            foreach (char character in id)
            {
                if (!(character >= 'a' && character <= 'z') && !(character >= '0' && character <= '9') && character != '-')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateSampleEntries(string[] entries, string label, string requiredPrefix)
        {
            if (entries == null)
            {
                return true;
            }

            foreach (string entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry) || !entry.StartsWith(requiredPrefix, StringComparison.Ordinal))
                {
                    Debug.LogError($"{label} 示例登记项无效：{entry}");
                    return false;
                }
            }

            return true;
        }

        private static bool TryApplyAppConfigs(SamplePackageManifest manifest, out SampleAppConfigsSnapshot before,
            out SampleAppConfigsSnapshot after, out SampleAppConfigsProfileBackup profileBackup, out string message)
        {
            before = null;
            after = null;
            profileBackup = null;
            if (manifest.appConfigs == null && manifest.appConfigsProfile == null)
            {
                message = null;
                return true;
            }

            if (!EnsureNoAppConfigsProfileConflict(manifest, out message))
            {
                return false;
            }

            var appConfigs = AppConfigs.ReloadInstanceEditor();
            if (appConfigs == null)
            {
                message = "未找到 Core/AppConfigs，无法安装需要启动集成的示例。";
                return false;
            }

            before = CaptureAppConfigs(appConfigs);
            if (manifest.appConfigsProfile != null)
            {
                try
                {
                    profileBackup = CreateAppConfigsProfileBackup(manifest, before);
                    ApplyAppConfigsProfile(appConfigs, manifest.appConfigsProfile);
                    after = CaptureAppConfigs(appConfigs);
                    profileBackup.applied = after;
                    profileBackup.phase = "Applied";
                    WriteAppConfigsProfileState(profileBackup);
                    message = null;
                    return true;
                }
                catch (Exception exception)
                {
                    message = $"无法切换 AppConfigs 配置档：{exception.Message}";
                    return false;
                }
            }

            var serializedObject = new SerializedObject(appConfigs);
            Undo.RecordObject(appConfigs, "Install optional sample AppConfigs");
            AppendUnique(serializedObject.FindProperty("mDataTables"), manifest.appConfigs.dataTables);
            AppendUnique(serializedObject.FindProperty("mConfigs"), manifest.appConfigs.configs);
            AppendUnique(serializedObject.FindProperty("mLanguages"), manifest.appConfigs.languages);
            AppendUnique(serializedObject.FindProperty("mProcedures"), manifest.appConfigs.procedures);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(appConfigs);
            AssetDatabase.SaveAssets();
            after = CaptureAppConfigs(appConfigs);
            message = null;
            return true;
        }

        private static bool TryValidateAppConfigs(SampleInstallRecord record, out string message)
        {
            if (record.appConfigsProfileBackup != null)
            {
                return TryValidateAppConfigsProfile(record.appConfigsProfileBackup, out message);
            }

            if (record.appConfigsAfter == null)
            {
                message = null;
                return true;
            }

            var appConfigs = AppConfigs.ReloadInstanceEditor();
            if (appConfigs == null)
            {
                message = "未找到已记录的 AppConfigs 资产。";
                return false;
            }

            SampleAppConfigsSnapshot current = CaptureAppConfigs(appConfigs);
            if (!string.Equals(current.sha256, record.appConfigsAfter.sha256, StringComparison.OrdinalIgnoreCase))
            {
                message = "AppConfigs 已在示例安装后被修改；为保护共享配置，已停止自动操作。";
                return false;
            }

            message = null;
            return true;
        }

        private static bool TryApplyBuildSettings(SamplePackageManifest manifest, out SampleBuildSettingsSnapshot before,
            out SampleBuildSettingsSnapshot after, out string message)
        {
            before = null;
            after = null;
            if (!manifest.addEntrySceneToBuildSettings)
            {
                message = null;
                return true;
            }

            string scenePath = ResolveProjectPath(manifest.entryScene);
            if (!File.Exists(scenePath))
            {
                message = $"需要加入 Build Settings 的入口场景不存在：{manifest.entryScene}";
                return false;
            }

            SampleBuildSettingsSnapshot current = CaptureBuildSettings();
            if (ContainsBuildSettingsScene(current, manifest.entryScene))
            {
                message = null;
                return true;
            }

            before = current;
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(manifest.entryScene, true),
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            after = CaptureBuildSettings();
            if (!ContainsBuildSettingsScene(after, manifest.entryScene))
            {
                throw new InvalidOperationException($"未能将入口场景加入 Build Settings：{manifest.entryScene}");
            }

            message = null;
            return true;
        }

        private static bool TryValidateBuildSettings(SampleInstallRecord record, out string message)
        {
            if (record.buildSettingsAfter == null)
            {
                message = null;
                return true;
            }

            SampleBuildSettingsSnapshot current = CaptureBuildSettings();
            if (!string.Equals(current.sha256, record.buildSettingsAfter.sha256, StringComparison.OrdinalIgnoreCase))
            {
                message = "Build Settings 已在样例安装后被修改；为保护当前项目场景列表，已停止自动操作。";
                return false;
            }

            message = null;
            return true;
        }

        private static void RestoreBuildSettings(SampleBuildSettingsSnapshot snapshot)
        {
            if (!HasBuildSettingsHash(snapshot))
            {
                throw new InvalidOperationException("Build Settings 快照不完整。");
            }

            var scenes = new EditorBuildSettingsScene[snapshot.scenes.Length];
            for (int index = 0; index < snapshot.scenes.Length; index++)
            {
                SampleBuildSettingsScene scene = snapshot.scenes[index];
                if (scene == null || string.IsNullOrWhiteSpace(scene.path))
                {
                    throw new InvalidOperationException("Build Settings 快照包含无效场景项。");
                }

                scenes[index] = new EditorBuildSettingsScene(scene.path, scene.enabled);
            }

            EditorBuildSettings.scenes = scenes;
            SampleBuildSettingsSnapshot current = CaptureBuildSettings();
            if (!string.Equals(current.sha256, snapshot.sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("恢复后的 Build Settings 校验失败。");
            }
        }

        private static SampleBuildSettingsSnapshot CaptureBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>();
            var entries = new SampleBuildSettingsScene[scenes.Length];
            var content = new StringBuilder();
            for (int index = 0; index < scenes.Length; index++)
            {
                entries[index] = new SampleBuildSettingsScene
                {
                    path = scenes[index].path,
                    enabled = scenes[index].enabled,
                };
                content.Append(scenes[index].path).Append('|').Append(scenes[index].enabled ? '1' : '0').Append('\n');
            }

            return new SampleBuildSettingsSnapshot
            {
                scenes = entries,
                sha256 = ComputeTextSha256(content.ToString()),
            };
        }

        private static bool ContainsBuildSettingsScene(SampleBuildSettingsSnapshot snapshot, string scenePath)
        {
            foreach (SampleBuildSettingsScene scene in snapshot.scenes)
            {
                if (string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryValidateAppConfigsProfile(SampleAppConfigsProfileBackup backup, out string message)
        {
            if (backup.applied == null || string.IsNullOrWhiteSpace(backup.applied.sha256))
            {
                message = "完整 AppConfigs 配置档缺少安装后状态记录。";
                return false;
            }

            string backupPath;
            try
            {
                backupPath = ResolveProjectPath(backup.backupAssetPath);
            }
            catch (Exception exception)
            {
                message = $"AppConfigs 备份路径无效：{exception.Message}";
                return false;
            }

            if (!File.Exists(backupPath) ||
                !string.Equals(backup.backupSha256, ComputeSha256(backupPath), StringComparison.OrdinalIgnoreCase))
            {
                message = "AppConfigs 安装前备份缺失或已被修改；已停止自动操作。";
                return false;
            }

            var appConfigs = AppConfigs.ReloadInstanceEditor();
            if (appConfigs == null)
            {
                message = "未找到已记录的 AppConfigs 资产。";
                return false;
            }

            SampleAppConfigsSnapshot current = CaptureAppConfigs(appConfigs);
            if (!string.Equals(current.sha256, backup.applied.sha256, StringComparison.OrdinalIgnoreCase))
            {
                message = "样例配置档激活期间 AppConfigs 已被修改；为保护当前项目配置，已停止自动操作。";
                return false;
            }

            message = null;
            return true;
        }

        private static void RestoreAppConfigs(SampleAppConfigsSnapshot snapshot)
        {
            var appConfigs = AppConfigs.ReloadInstanceEditor();
            if (appConfigs == null)
            {
                throw new InvalidOperationException("未找到已记录的 AppConfigs 资产。");
            }

            var serializedObject = new SerializedObject(appConfigs);
            Undo.RecordObject(appConfigs, "Restore optional sample AppConfigs");
            serializedObject.FindProperty("m_LoadFromBytes").boolValue = snapshot.loadFromBytes;
            ReplaceEntries(serializedObject.FindProperty("mDataTables"), snapshot.dataTables);
            ReplaceEntries(serializedObject.FindProperty("mConfigs"), snapshot.configs);
            ReplaceEntries(serializedObject.FindProperty("mLanguages"), snapshot.languages);
            ReplaceEntries(serializedObject.FindProperty("mProcedures"), snapshot.procedures);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(appConfigs);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyAppConfigsProfile(AppConfigs appConfigs, SampleAppConfigsProfile profile)
        {
            var serializedObject = new SerializedObject(appConfigs);
            Undo.RecordObject(appConfigs, "Activate optional sample AppConfigs profile");
            SerializedProperty loadFromBytes = serializedObject.FindProperty("m_LoadFromBytes");
            loadFromBytes.boolValue = profile.loadFromBytes;
            ReplaceEntries(serializedObject.FindProperty("mDataTables"), profile.dataTables);
            ReplaceEntries(serializedObject.FindProperty("mConfigs"), profile.configs);
            ReplaceEntries(serializedObject.FindProperty("mLanguages"), profile.languages);
            ReplaceEntries(serializedObject.FindProperty("mProcedures"), profile.procedures);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(appConfigs);
            AssetDatabase.SaveAssets();
        }

        private static SampleAppConfigsProfileBackup CreateAppConfigsProfileBackup(SamplePackageManifest manifest,
            SampleAppConfigsSnapshot before)
        {
            string stateDirectory = GetAppConfigsProfileStateDirectory(manifest.id);
            if (Directory.Exists(stateDirectory))
            {
                throw new InvalidOperationException(
                    $"发现未清理的 AppConfigs 配置档状态：{ToProjectRelativePath(stateDirectory)}。请先执行恢复操作。");
            }

            string assetPath = AssetDatabase.GetAssetPath(AppConfigs.ReloadInstanceEditor());
            string sourceAssetPath = ResolveProjectPath(assetPath);
            Directory.CreateDirectory(stateDirectory);
            string backupPath = Path.Combine(stateDirectory, AppConfigsBackupFileName);
            File.Copy(sourceAssetPath, backupPath, false);

            var backup = new SampleAppConfigsProfileBackup
            {
                stateDirectory = ToProjectRelativePath(stateDirectory),
                backupAssetPath = ToProjectRelativePath(backupPath),
                assetPath = assetPath,
                backupSha256 = ComputeSha256(backupPath),
                before = before,
                phase = "BackedUp",
            };
            WriteAppConfigsProfileState(backup);
            return backup;
        }

        private static void RestoreAppConfigsProfile(SampleAppConfigsProfileBackup backup)
        {
            if (backup == null || backup.before == null || string.IsNullOrWhiteSpace(backup.assetPath))
            {
                throw new InvalidOperationException("AppConfigs 配置档备份不完整。");
            }

            string backupPath = ResolveProjectPath(backup.backupAssetPath);
            if (!File.Exists(backupPath) ||
                !string.Equals(backup.backupSha256, ComputeSha256(backupPath), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("AppConfigs 安装前备份缺失或已被修改。");
            }

            string targetPath = ResolveProjectPath(backup.assetPath);
            string temporaryPath = targetPath + ".sample-restore.tmp";
            try
            {
                File.Copy(backupPath, temporaryPath, true);
                File.Copy(temporaryPath, targetPath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            AssetDatabase.ImportAsset(backup.assetPath, ImportAssetOptions.ForceUpdate);
            AppConfigs restored = AppConfigs.ReloadInstanceEditor();
            if (restored == null)
            {
                throw new InvalidOperationException("恢复 AppConfigs 后无法重新加载该资产。");
            }

            SampleAppConfigsSnapshot current = CaptureAppConfigs(restored);
            if (!string.Equals(current.sha256, backup.before.sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("恢复后的 AppConfigs 校验失败。");
            }
        }

        private static bool EnsureNoAppConfigsProfileConflict(SamplePackageManifest manifest, out string message)
        {
            if (manifest.appConfigs == null && manifest.appConfigsProfile == null)
            {
                message = null;
                return true;
            }

            string samplesRoot = ResolveProjectPath(SamplesRootRelativePath);
            if (!Directory.Exists(samplesRoot))
            {
                message = null;
                return true;
            }

            foreach (string packageDirectory in Directory.GetDirectories(samplesRoot))
            {
                SamplePackageInfo installedPackage = LoadPackage(packageDirectory);
                if (!installedPackage.IsValid || string.Equals(installedPackage.Manifest.id, manifest.id, StringComparison.Ordinal))
                {
                    continue;
                }

                SampleInstallRecord record = ReadInstallRecord(installedPackage.Manifest);
                if (record?.appConfigsProfileBackup != null || record?.appConfigsAfter != null)
                {
                    message = $"“{installedPackage.Manifest.displayName}”正在管理 AppConfigs。请先卸载它，再安装需要修改 AppConfigs 的示例。";
                    return false;
                }
            }

            message = null;
            return true;
        }

        internal static bool HasPendingAppConfigsProfileRecovery(SamplePackageInfo package)
        {
            if (!package.IsValid || IsInstalled(package))
            {
                return false;
            }

            return File.Exists(Path.Combine(GetAppConfigsProfileStateDirectory(package.Manifest.id), AppConfigsProfileStateFileName));
        }

        internal static bool TryRecoverPendingAppConfigsProfile(SamplePackageInfo package, out string message)
        {
            if (!EnsureEditorReadyForPackageMutation(out message))
            {
                return false;
            }

            if (!package.IsValid)
            {
                message = package.Error ?? "示例包无效。";
                return false;
            }

            string statePath = Path.Combine(GetAppConfigsProfileStateDirectory(package.Manifest.id), AppConfigsProfileStateFileName);
            if (!File.Exists(statePath))
            {
                message = "未找到待恢复的 AppConfigs 配置档状态。";
                return false;
            }

            try
            {
                var backup = JsonUtility.FromJson<SampleAppConfigsProfileBackup>(File.ReadAllText(statePath));
                RestoreAppConfigsProfile(backup);
                DeleteAppConfigsProfileState(backup);
                message = "已恢复安装前的 AppConfigs 配置，并清理中断安装状态。";
                return true;
            }
            catch (Exception exception)
            {
                message = $"恢复 AppConfigs 配置档失败：{exception.Message}";
                return false;
            }
        }

        private static string GetAppConfigsProfileStateDirectory(string packageId)
        {
            return ResolvePathInsideRoot(ProjectRootPath,
                Path.Combine(SampleStateRootRelativePath, packageId), "示例配置档状态路径");
        }

        private static bool EnsureEditorReadyForPackageMutation(out string message)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                message = "请先退出播放模式，再安装、修复、卸载或恢复示例包。";
                return false;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                message = "Unity 正在编译或刷新资产，请稍后再操作示例包。";
                return false;
            }

            message = null;
            return true;
        }

        private static void WriteAppConfigsProfileState(SampleAppConfigsProfileBackup backup)
        {
            string stateDirectory = ResolveProjectPath(backup.stateDirectory);
            Directory.CreateDirectory(stateDirectory);
            string statePath = Path.Combine(stateDirectory, AppConfigsProfileStateFileName);
            string temporaryPath = statePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(backup, true));
            File.Copy(temporaryPath, statePath, true);
            File.Delete(temporaryPath);
        }

        private static void DeleteAppConfigsProfileState(SampleAppConfigsProfileBackup backup)
        {
            string stateDirectory = ResolveProjectPath(backup.stateDirectory);
            string stateRoot = ResolveProjectPath(SampleStateRootRelativePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!stateDirectory.StartsWith(stateRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("拒绝清理配置档状态目录：路径超出受控范围。");
            }

            if (Directory.Exists(stateDirectory))
            {
                Directory.Delete(stateDirectory, true);
            }
        }

        private static SampleAppConfigsSnapshot CaptureAppConfigs(AppConfigs appConfigs)
        {
            string assetPath = AssetDatabase.GetAssetPath(appConfigs);
            return new SampleAppConfigsSnapshot
            {
                loadFromBytes = appConfigs.LoadFromBytes,
                dataTables = CloneEntries(appConfigs.DataTables),
                configs = CloneEntries(appConfigs.Configs),
                languages = CloneEntries(appConfigs.Languages),
                procedures = CloneEntries(appConfigs.Procedures),
                sha256 = ComputeSha256(ResolveProjectPath(assetPath)),
            };
        }

        private static string[] CloneEntries(string[] entries)
        {
            return entries == null ? Array.Empty<string>() : (string[])entries.Clone();
        }

        private static void AppendUnique(SerializedProperty property, string[] entries)
        {
            if (entries == null)
            {
                return;
            }

            foreach (string entry in entries)
            {
                bool exists = false;
                for (int index = 0; index < property.arraySize; index++)
                {
                    if (string.Equals(property.GetArrayElementAtIndex(index).stringValue, entry, StringComparison.Ordinal))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    property.InsertArrayElementAtIndex(property.arraySize);
                    property.GetArrayElementAtIndex(property.arraySize - 1).stringValue = entry;
                }
            }
        }

        private static void ReplaceEntries(SerializedProperty property, string[] entries)
        {
            property.arraySize = entries?.Length ?? 0;
            for (int index = 0; index < property.arraySize; index++)
            {
                property.GetArrayElementAtIndex(index).stringValue = entries[index];
            }
        }

        private static string GetInstallRecordPath(SamplePackageManifest manifest)
        {
            return Path.Combine(ResolveProjectPath(manifest.installRoot), InstallRecordFileName);
        }

        private static void DeleteInstallRecord(SamplePackageManifest manifest)
        {
            string recordPath = GetInstallRecordPath(manifest);
            if (File.Exists(recordPath))
            {
                File.Delete(recordPath);
            }
        }

        private static bool IsDeclaredDestinationPath(SamplePackageManifest manifest, string projectRelativePath)
        {
            string normalizedPath = projectRelativePath.Replace('\\', '/').TrimStart('/');
            if (string.Equals(normalizedPath, manifest.installRoot.Replace('\\', '/').TrimEnd('/') + "/" + InstallRecordFileName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (SamplePayloadMapping payload in manifest.payloads)
            {
                string destinationRoot = payload.destination.Replace('\\', '/').TrimEnd('/') + "/";
                if (normalizedPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ResolvePackagePath(string packageDirectory, string relativePath)
        {
            return ResolvePathInsideRoot(packageDirectory, relativePath, "示例包路径");
        }

        private static string ResolveProjectPath(string relativePath)
        {
            return ResolvePathInsideRoot(ProjectRootPath, relativePath, "项目路径");
        }

        private static string ResolvePathInsideRoot(string rootPath, string relativePath, string label)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException($"{label} cannot be empty.");
            }

            string fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
            string normalizedRoot = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"{label} escapes its allowed root: {relativePath}");
            }

            return fullPath;
        }

        private static string ToProjectRelativePath(string fullPath)
        {
            return GetRelativePath(ProjectRootPath, fullPath).Replace('\\', '/');
        }

        private static string GetRelativePath(string rootPath, string fullPath)
        {
            Uri rootUri = new Uri(Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);
            Uri fileUri = new Uri(Path.GetFullPath(fullPath));
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string ComputeSha256(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(filePath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static string ComputeTextSha256(string content)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static string ProjectRootPath => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }
}
