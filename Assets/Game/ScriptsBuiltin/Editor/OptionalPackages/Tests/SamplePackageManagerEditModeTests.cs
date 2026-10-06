using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace AiFriendlyFrame.Editor.Samples.Tests
{
    public sealed class SamplePackageManagerEditModeTests
    {
        private const string TestPackageId = "sample-manager-test";
        private string _packageDirectory;
        private SamplePackageInfo _package;
        private string[] _dataTablesBefore;
        private string[] _configsBefore;
        private string[] _languagesBefore;

        [SetUp]
        public void SetUp()
        {
            AppConfigs appConfigs = AppConfigs.ReloadInstanceEditor();
            _dataTablesBefore = (string[])appConfigs.DataTables.Clone();
            _configsBefore = (string[])appConfigs.Configs.Clone();
            _languagesBefore = (string[])appConfigs.Languages.Clone();
            _packageDirectory = Path.Combine(ProjectRoot, SamplePackageManager.SamplesRootRelativePath, "__SampleManagerTest");
            if (Directory.Exists(_packageDirectory))
            {
                Directory.Delete(_packageDirectory, true);
            }

            string payloadDirectory = Path.Combine(_packageDirectory, "payload", "Assets", "Sample", "SampleManagerTest");
            Directory.CreateDirectory(payloadDirectory);
            File.WriteAllText(Path.Combine(payloadDirectory, "marker.txt"), "Optional Sample Manager test payload.");
            File.WriteAllText(Path.Combine(payloadDirectory, "marker.txt.meta"), "fileFormatVersion: 2\nguid: 0bb5d6097729468b8b19f6ce6a538bcd\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n");
            File.WriteAllText(Path.Combine(_packageDirectory, "sample.json"), "{\n" +
                "  \"id\": \"sample-manager-test\",\n" +
                "  \"displayName\": \"Sample Manager Test\",\n" +
                "  \"version\": \"1.0.0\",\n" +
                "  \"entryScene\": \"Assets/Sample/SampleManagerTest/Test.unity\",\n" +
                "  \"installRoot\": \"Assets/Sample/SampleManagerTest\",\n" +
                "  \"payloads\": [{ \"source\": \"payload/Assets/Sample/SampleManagerTest\", \"destination\": \"Assets/Sample/SampleManagerTest\" }],\n" +
                "  \"appConfigs\": {\n" +
                "    \"dataTables\": [\"Sample/SampleManagerTest/TestTable\"],\n" +
                "    \"configs\": [\"Sample/SampleManagerTest/TestConfig\"],\n" +
                "    \"languages\": [\"Sample/SampleManagerTest\"]\n" +
                "  }\n" +
                "}");

            AssetDatabase.Refresh();
            foreach (SamplePackageInfo package in SamplePackageManager.DiscoverPackages())
            {
                if (package.IsValid && package.Manifest.id == TestPackageId)
                {
                    _package = package;
                    break;
                }
            }

            Assert.IsTrue(_package.IsValid, "The temporary Sample package should be discoverable.");
            if (SamplePackageManager.IsInstalled(_package))
            {
                Assert.IsTrue(SamplePackageManager.TryUninstall(_package, true, out string message), message);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_package.IsValid && SamplePackageManager.IsInstalled(_package))
            {
                SamplePackageManager.TryUninstall(_package, true, out _);
            }

            if (!string.IsNullOrEmpty(_packageDirectory) && Directory.Exists(_packageDirectory))
            {
                Directory.Delete(_packageDirectory, true);
            }

            AssetDatabase.Refresh();
        }

        [Test]
        public void InstallValidateAndUninstall_OnlyTouchesManifestFiles()
        {
            Assert.IsTrue(SamplePackageManager.TryInstall(_package, out string installMessage), installMessage);
            Assert.IsTrue(SamplePackageManager.IsInstalled(_package));
            Assert.IsTrue(SamplePackageManager.TryValidate(_package, out string validationMessage), validationMessage);

            string markerPath = Path.Combine(ProjectRoot, "Assets", "Sample", "SampleManagerTest", "marker.txt");
            Assert.IsTrue(File.Exists(markerPath));

            Assert.IsTrue(SamplePackageManager.TryUninstall(_package, false, out string uninstallMessage), uninstallMessage);
            Assert.IsFalse(SamplePackageManager.IsInstalled(_package));
            Assert.IsFalse(File.Exists(markerPath));

            string sampleRoot = Path.Combine(ProjectRoot, "Assets", "Sample");
            Assert.IsFalse(Directory.Exists(sampleRoot));
            Assert.IsFalse(File.Exists(sampleRoot + ".meta"));
        }

        [Test]
        public void InstallAndUninstall_RestoresAppConfigsRegistration()
        {
            Assert.IsTrue(SamplePackageManager.TryInstall(_package, out string installMessage), installMessage);

            AppConfigs installedConfigs = AppConfigs.ReloadInstanceEditor();
            CollectionAssert.Contains(installedConfigs.DataTables, "Sample/SampleManagerTest/TestTable");
            CollectionAssert.Contains(installedConfigs.Configs, "Sample/SampleManagerTest/TestConfig");
            CollectionAssert.Contains(installedConfigs.Languages, "Sample/SampleManagerTest");

            Assert.IsTrue(SamplePackageManager.TryUninstall(_package, false, out string uninstallMessage), uninstallMessage);
            AppConfigs restoredConfigs = AppConfigs.ReloadInstanceEditor();
            CollectionAssert.AreEquivalent(_dataTablesBefore, restoredConfigs.DataTables);
            CollectionAssert.AreEquivalent(_configsBefore, restoredConfigs.Configs);
            CollectionAssert.AreEquivalent(_languagesBefore, restoredConfigs.Languages);
        }

        [Test]
        public void ModifiedInstallation_RequiresForceRemovalOrRepair()
        {
            Assert.IsTrue(SamplePackageManager.TryInstall(_package, out string installMessage), installMessage);
            string markerPath = Path.Combine(ProjectRoot, "Assets", "Sample", "SampleManagerTest", "marker.txt");
            File.AppendAllText(markerPath, "\nUser modification.");

            Assert.IsFalse(SamplePackageManager.TryValidate(_package, out _));
            Assert.IsFalse(SamplePackageManager.TryUninstall(_package, false, out _));

            Assert.IsTrue(SamplePackageManager.TryRepair(_package, out string repairMessage), repairMessage);
            Assert.IsTrue(SamplePackageManager.TryValidate(_package, out string validationMessage), validationMessage);
            Assert.IsTrue(SamplePackageManager.TryUninstall(_package, false, out string uninstallMessage), uninstallMessage);
        }

        [Test]
        public void ExistingDestinationFile_IsNeverClaimedByInstallation()
        {
            string markerPath = Path.Combine(ProjectRoot, "Assets", "Sample", "SampleManagerTest", "marker.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(markerPath));
            File.WriteAllText(markerPath, "Pre-existing user file.");

            Assert.IsFalse(SamplePackageManager.TryInstall(_package, out string message));
            StringAssert.Contains("安装冲突", message);
            Assert.IsTrue(File.Exists(markerPath));
            File.Delete(markerPath);
        }

        [Test]
        public void BasicUiPackage_IsDiscoverableWithoutBeingInstalled()
        {
            bool found = false;
            foreach (SamplePackageInfo package in SamplePackageManager.DiscoverPackages())
            {
                if (package.IsValid && package.Manifest.id == "basic-ui")
                {
                    found = true;
                    Assert.AreEqual("Assets/Sample/BasicUi/Scenes/BasicUiSample.unity", package.Manifest.entryScene);
                    Assert.IsFalse(SamplePackageManager.IsInstalled(package));
                    break;
                }
            }

            Assert.IsTrue(found, "BasicUi should be available as an optional package source.");
        }

        [Test]
        public void CircuitPuzzlePackage_IsDiscoverableWithoutBeingInstalled()
        {
            bool found = false;
            foreach (SamplePackageInfo package in SamplePackageManager.DiscoverPackages())
            {
                if (package.IsValid && package.Manifest.id == "circuit-puzzle")
                {
                    found = true;
                    Assert.AreEqual("Assets/Sample/CircuitPuzzle/Scenes/CircuitPuzzle.unity", package.Manifest.entryScene);
                    Assert.IsFalse(SamplePackageManager.IsInstalled(package));
                    break;
                }
            }

            Assert.IsTrue(found, "Circuit Puzzle should be available as an optional package source.");
        }

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
    }
}
