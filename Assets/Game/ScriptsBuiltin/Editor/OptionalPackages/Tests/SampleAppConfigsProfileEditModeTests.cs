using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AiFriendlyFrame.Editor.Samples.Tests
{
    public sealed class SampleAppConfigsProfileEditModeTests
    {
        private const string PackageId = "sample-profile-manager-test";
        private const string SourceRelativePath = "Samples~/__SampleProfileManagerTest";
        private const string InstallRelativePath = "Assets/Sample/SampleProfileManagerTest";
        private const string StateRelativePath = ".ai-friendly-frame/sample-state/" + PackageId;
        private const string AppConfigsAssetPath = "Assets/Game/ScriptableAssets/Core/AppConfigs.asset";

        [SetUp]
        public void SetUp()
        {
            DeleteAssetPath(InstallRelativePath);
            DeleteProjectDirectory(SourceRelativePath);
            DeleteProjectDirectory(StateRelativePath);
            CreateTestPackage();
            SamplePackageManager.DiscoverPackages();
        }

        [TearDown]
        public void TearDown()
        {
            var package = FindPackage();
            if (package.HasValue && SamplePackageManager.IsInstalled(package.Value))
            {
                SamplePackageManager.TryUninstall(package.Value, true, out _);
            }

            DeleteAssetPath(InstallRelativePath);
            DeleteProjectDirectory(SourceRelativePath);
            DeleteProjectDirectory(StateRelativePath);
            AssetDatabase.Refresh();
            SamplePackageManager.DiscoverPackages();
        }

        [Test]
        public void FullProfile_InstallValidateAndUninstall_RestoresOriginalAsset()
        {
            var originalBytes = File.ReadAllBytes(ToAbsoluteProjectPath(AppConfigsAssetPath));
            var package = RequirePackage();

            Assert.That(SamplePackageManager.TryInstall(package, out var installMessage), Is.True, installMessage);
            Assert.That(SamplePackageManager.IsInstalled(package), Is.True);
            Assert.That(Directory.Exists(ToAbsoluteProjectPath(StateRelativePath)), Is.True);
            Assert.That(SamplePackageManager.TryValidate(package, out var validateMessage), Is.True, validateMessage);

            Assert.That(SamplePackageManager.TryUninstall(package, false, out var uninstallMessage), Is.True, uninstallMessage);
            CollectionAssert.AreEqual(originalBytes, File.ReadAllBytes(ToAbsoluteProjectPath(AppConfigsAssetPath)));
            Assert.That(Directory.Exists(ToAbsoluteProjectPath(StateRelativePath)), Is.False);
        }

        [Test]
        public void InterruptedProfileInstall_CanRestoreOriginalAppConfigs()
        {
            var originalBytes = File.ReadAllBytes(ToAbsoluteProjectPath(AppConfigsAssetPath));
            var package = RequirePackage();

            Assert.That(SamplePackageManager.TryInstall(package, out var installMessage), Is.True, installMessage);
            File.Delete(ToAbsoluteProjectPath(InstallRelativePath + "/.sample-install.json"));
            AssetDatabase.Refresh();

            Assert.That(SamplePackageManager.HasPendingAppConfigsProfileRecovery(package), Is.True);
            Assert.That(SamplePackageManager.TryRecoverPendingAppConfigsProfile(package, out var recoveryMessage), Is.True, recoveryMessage);
            CollectionAssert.AreEqual(originalBytes, File.ReadAllBytes(ToAbsoluteProjectPath(AppConfigsAssetPath)));
            Assert.That(Directory.Exists(ToAbsoluteProjectPath(StateRelativePath)), Is.False);
        }

        private static SamplePackageInfo RequirePackage()
        {
            SamplePackageInfo? package = FindPackage();
            if (package.HasValue)
            {
                return package.Value;
            }

            Assert.Fail("Temporary test package was not discovered.");
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
            var absoluteSourcePath = ToAbsoluteProjectPath(SourceRelativePath);
            Directory.CreateDirectory(Path.Combine(absoluteSourcePath, "Payload"));
            File.WriteAllBytes(Path.Combine(absoluteSourcePath, "Payload", "marker.bin"), new byte[] { 0x54, 0x65, 0x73, 0x74 });

            File.WriteAllText(Path.Combine(absoluteSourcePath, "sample.json"), "{\n" +
                "  \"id\": \"" + PackageId + "\",\n" +
                "  \"displayName\": \"Profile manager test\",\n" +
                "  \"version\": \"1.0.0\",\n" +
                "  \"entryScene\": \"" + InstallRelativePath + "/Test.unity\",\n" +
                "  \"installRoot\": \"" + InstallRelativePath + "\",\n" +
                "  \"payloads\": [{ \"source\": \"Payload\", \"destination\": \"" + InstallRelativePath + "\" }],\n" +
                "  \"appConfigsProfile\": {\n" +
                "    \"loadFromBytes\": true,\n" +
                "    \"dataTables\": [\"Core/LanguagesTable\"],\n" +
                "    \"configs\": [],\n" +
                "    \"languages\": [\"English\"],\n" +
                "    \"procedures\": [\"PreloadProcedure\"]\n" +
                "  }\n" +
                "}");
            AssetDatabase.Refresh();
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
            var absolutePath = ToAbsoluteProjectPath(relativePath);
            if (Directory.Exists(absolutePath))
            {
                Directory.Delete(absolutePath, true);
            }
        }
    }
}
