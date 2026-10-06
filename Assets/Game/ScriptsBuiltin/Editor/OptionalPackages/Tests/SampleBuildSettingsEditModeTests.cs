using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AiFriendlyFrame.Editor.Samples.Tests
{
    public sealed class SampleBuildSettingsEditModeTests
    {
        private const string PackageId = "sample-build-settings-test";
        private const string SourceRelativePath = "Samples~/__SampleBuildSettingsTest";
        private const string InstallRelativePath = "Assets/Sample/SampleBuildSettingsTest";
        private const string EntryScenePath = InstallRelativePath + "/BuildSettingsTest.unity";

        private EditorBuildSettingsScene[] _buildSettingsBefore;

        [SetUp]
        public void SetUp()
        {
            _buildSettingsBefore = CloneBuildSettings(EditorBuildSettings.scenes);
            DeleteAssetPath(InstallRelativePath);
            DeleteProjectDirectory(SourceRelativePath);
            CreateTestPackage();
        }

        [TearDown]
        public void TearDown()
        {
            var package = FindPackage();
            if (package.HasValue && SamplePackageManager.IsInstalled(package.Value))
            {
                SamplePackageManager.TryUninstall(package.Value, true, out _);
            }

            EditorBuildSettings.scenes = _buildSettingsBefore;
            DeleteAssetPath(InstallRelativePath);
            DeleteProjectDirectory(SourceRelativePath);
            AssetDatabase.Refresh();
        }

        [Test]
        public void InstallValidateAndUninstall_RestoresBuildSettings()
        {
            SamplePackageInfo package = RequirePackage();
            Assert.That(ContainsScene(EditorBuildSettings.scenes, EntryScenePath), Is.False);

            Assert.That(SamplePackageManager.TryInstall(package, out var installMessage), Is.True, installMessage);
            Assert.That(ContainsScene(EditorBuildSettings.scenes, EntryScenePath), Is.True);
            Assert.That(SamplePackageManager.TryValidate(package, out var validateMessage), Is.True, validateMessage);

            Assert.That(SamplePackageManager.TryUninstall(package, false, out var uninstallMessage), Is.True, uninstallMessage);
            AssertBuildSettingsEqual(_buildSettingsBefore, EditorBuildSettings.scenes);
        }

        [Test]
        public void ChangedBuildSettings_BlocksAutomaticUninstall()
        {
            SamplePackageInfo package = RequirePackage();
            Assert.That(SamplePackageManager.TryInstall(package, out var installMessage), Is.True, installMessage);

            EditorBuildSettings.scenes = Array.Empty<EditorBuildSettingsScene>();
            Assert.That(SamplePackageManager.TryUninstall(package, true, out var uninstallMessage), Is.False);
            StringAssert.Contains("Build Settings 已在样例安装后被修改", uninstallMessage);
            Assert.That(SamplePackageManager.IsInstalled(package), Is.True);
        }

        private static SamplePackageInfo RequirePackage()
        {
            SamplePackageInfo? package = FindPackage();
            if (package.HasValue)
            {
                return package.Value;
            }

            Assert.Fail("Temporary Build Settings Sample package was not discovered.");
            return default;
        }

        private static SamplePackageInfo? FindPackage()
        {
            foreach (SamplePackageInfo package in SamplePackageManager.DiscoverPackages())
            {
                if (package.IsValid && string.Equals(package.Manifest.id, PackageId, StringComparison.Ordinal))
                {
                    return package;
                }
            }

            return null;
        }

        private static void CreateTestPackage()
        {
            string sourceDirectory = ToAbsoluteProjectPath(SourceRelativePath);
            string payloadDirectory = Path.Combine(sourceDirectory, "payload", "Assets", "Sample", "SampleBuildSettingsTest");
            Directory.CreateDirectory(payloadDirectory);
            File.Copy(ToAbsoluteProjectPath("Assets/Game/Scene/Launch.unity"),
                Path.Combine(payloadDirectory, "BuildSettingsTest.unity"), false);

            File.WriteAllText(Path.Combine(sourceDirectory, "sample.json"), "{\n" +
                "  \"id\": \"" + PackageId + "\",\n" +
                "  \"displayName\": \"Build Settings test\",\n" +
                "  \"version\": \"1.0.0\",\n" +
                "  \"entryScene\": \"" + EntryScenePath + "\",\n" +
                "  \"installRoot\": \"" + InstallRelativePath + "\",\n" +
                "  \"addEntrySceneToBuildSettings\": true,\n" +
                "  \"payloads\": [{ \"source\": \"payload/Assets/Sample/SampleBuildSettingsTest\", \"destination\": \"" + InstallRelativePath + "\" }]\n" +
                "}");
            AssetDatabase.Refresh();
        }

        private static EditorBuildSettingsScene[] CloneBuildSettings(EditorBuildSettingsScene[] scenes)
        {
            var cloned = new EditorBuildSettingsScene[scenes.Length];
            for (int index = 0; index < scenes.Length; index++)
            {
                cloned[index] = new EditorBuildSettingsScene(scenes[index].path, scenes[index].enabled);
            }

            return cloned;
        }

        private static bool ContainsScene(EditorBuildSettingsScene[] scenes, string scenePath)
        {
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertBuildSettingsEqual(EditorBuildSettingsScene[] expected, EditorBuildSettingsScene[] actual)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int index = 0; index < expected.Length; index++)
            {
                Assert.That(actual[index].path, Is.EqualTo(expected[index].path));
                Assert.That(actual[index].enabled, Is.EqualTo(expected[index].enabled));
            }
        }

        private static string ToAbsoluteProjectPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static void DeleteAssetPath(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static void DeleteProjectDirectory(string relativePath)
        {
            string absolutePath = ToAbsoluteProjectPath(relativePath);
            if (Directory.Exists(absolutePath))
            {
                Directory.Delete(absolutePath, true);
            }
        }
    }
}
