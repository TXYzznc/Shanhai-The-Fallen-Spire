using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Shanhai.Demo.Editor
{
    /// <summary>只读加载交付目录中的资源包，不实例化实体、不启动Player。仅卸载自己加载的包。</summary>
    internal static class DemoPackedAssetAcceptance
    {
        [Serializable] private sealed class FileEvidence { public string name; public string sha256; }
        private sealed class ResourceInfo { public int loadType, length, crc; }
        [Serializable] private sealed class Report
        {
            public string result, utc, unity, error;
            public string playerBuildResult, playerBuildReportModifiedUtc;
            public ulong playerBuildBytes;
            public List<string> checks = new List<string>();
            public List<FileEvidence> files = new List<FileEvidence>();
        }

        [MenuItem("Shanhai/Demo/Verify delivered resource bundles")]
        public static void Verify()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("检查资源包前先停止Play Mode并等待编译。");
            string project = Directory.GetParent(Application.dataPath).FullName;
            string build = Path.Combine(project, "BuildApp/ShanhaiDemoWindows");
            string working = Path.Combine(build, "ResourceBuild/Working/Windows64");
            string delivered = Path.Combine(build, "ShanhaiDemo_Data/StreamingAssets");
            var report = new Report { result = "Failed", utc = DateTime.UtcNow.ToString("O"), unity = Application.unityVersion };
            var owned = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);
            AssetBundle manifestBundle = null;
            try
            {
                foreach (AssetBundle existing in AssetBundle.GetAllLoadedAssetBundles())
                    if (existing != null) throw new InvalidOperationException("已有资源包加载，不能把未知版本当成待验包：" + existing.name);
                BuildReport buildReport = BuildReport.GetLatestReport();
                Require(buildReport != null && buildReport.summary.result == BuildResult.Succeeded
                    && buildReport.summary.totalErrors == 0 && string.Equals(Path.GetFullPath(buildReport.summary.outputPath),
                        Path.GetFullPath(Path.Combine(build, "ShanhaiDemo.exe")), StringComparison.OrdinalIgnoreCase), "最新报告不是此交付目录的成功Player构建。");
                report.playerBuildResult = buildReport.summary.result.ToString();
                report.playerBuildBytes = buildReport.summary.totalSize;
                report.playerBuildReportModifiedUtc = File.GetLastWriteTimeUtc(Path.Combine(project, "Library/LastBuild.buildreport")).ToString("O");
                manifestBundle = AssetBundle.LoadFromFile(Path.Combine(working, "Windows64"));
                Require(manifestBundle != null, "生成的Windows64 Manifest不能加载。");
                var manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                Require(manifest != null, "生成目录缺少AssetBundleManifest。");
                var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string file in Directory.GetFiles(delivered, "*.dat")) data.Add(Path.GetFileNameWithoutExtension(file), file);
                var resourceInfo = ResourceInfos(build);
                string[] names = manifest.GetAllAssetBundles(); Array.Sort(names, StringComparer.Ordinal);
                foreach (string name in names) Load(name, manifest, data, resourceInfo, owned, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                report.checks.Add("deliveredBundles=" + owned.Count);

                const string configPath = "Assets/Game/ScriptableAssets/Demo/DemoPresentationConfig.asset";
                DemoPresentationConfig config = Asset<DemoPresentationConfig>(owned, configPath);
                Require(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(config)), "读取到了源配置而非交付包配置。");
                var fields = new SerializedObject(config);
                foreach (string field in new[] { "menuPrefabAsset", "hudPrefabAsset", "combatOverlayPrefabAsset", "rewardPrefabAsset", "assemblyPrefabAsset", "completionPrefabAsset", "playerPrefabAsset", "commonEnemyPrefabAsset", "eliteEnemyPrefabAsset", "bossPrefabAsset" })
                {
                    string path = fields.FindProperty(field).stringValue;
                    GameObject prefab = Asset<GameObject>(owned, path); CheckPrefab(prefab, path, report);
                    if (field == "menuPrefabAsset") Require(prefab.GetComponent<DemoStartMenuForm>() != null, "开始界面逻辑缺失。");
                    if (field == "hudPrefabAsset") Require(prefab.GetComponent<DemoHudForm>() != null && prefab.GetComponent<DemoHudForm>().Display != null, "HUD显示区引用缺失。");
                    if (field == "combatOverlayPrefabAsset") Require(prefab.GetComponent<DemoCombatOverlayForm>() != null
                        && prefab.GetComponent<DemoCombatOverlayForm>().DamageLayer != null && prefab.GetComponent<DemoCombatOverlayForm>().EnemyStatusLayer != null, "独立表现层引用缺失。");
                    if (field == "rewardPrefabAsset") Require(prefab.GetComponent<DemoRewardForm>() != null, "奖励窗口逻辑缺失。");
                    if (field == "assemblyPrefabAsset") Require(prefab.GetComponent<DemoMarkAssemblyForm>() != null, "装配窗口逻辑缺失。");
                    if (field == "completionPrefabAsset") Require(prefab.GetComponent<DemoCompletionForm>() != null, "结算窗口逻辑缺失。");
                    if (field.EndsWith("PrefabAsset", StringComparison.Ordinal) && path.StartsWith("Assets/Game/Prefabs/Entity/", StringComparison.Ordinal))
                    {
                        Require(prefab.GetComponent<EntityLogic>() == null, "实体包预挂EntityLogic，GF会重复添加。");
                        var sprite = prefab.GetComponent<SpriteRenderer>();
                        Require(sprite != null && sprite.sprite != null && sprite.sharedMaterial != null, "实体贴图或材质缺失：" + path);
                        Require(sprite.sharedMaterial.shader != null && sprite.sharedMaterial.shader.name == "Shanhai/Demo/SpriteFeedback"
                            && sprite.sharedMaterial.shader.isSupported && sprite.sharedMaterial.HasProperty("_FlashAmount"), "实体包受击Shader失效：" + path);
                        Require(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(sprite.sharedMaterial)), "实体材质来自源目录而非包内。");
                        var enemy = prefab.GetComponent<Shanhai.Demo.Enemies.DemoEnemyController>();
                        if (enemy != null)
                        {
                            var status = prefab.GetComponent<DemoEnemyStatusBinding>();
                            Require(status != null && !status.enabled, "敌人状态绑定缺失或在会话绑定前启用：" + path);
                            var references = new SerializedObject(status);
                            Require(references.FindProperty("prefab").objectReferenceValue is DemoEnemyStatusItem
                                && references.FindProperty("enemy").objectReferenceValue == enemy
                                && references.FindProperty("health").objectReferenceValue == prefab.GetComponent<Shanhai.Demo.Combat.DemoDamageReceiver>(),
                                "敌人状态Item或内部引用错误：" + path);
                        }
                    }
                }
                Require(config.AttackArcPrefab != null && config.HitParticlePrefab != null && config.DamageTextPrefab != null, "包内配置的特效模板引用丢失。");
                foreach (GameObject prefab in new[] { config.AttackArcPrefab.gameObject, config.HitParticlePrefab.gameObject, config.DamageTextPrefab.gameObject })
                    CheckPrefab(prefab, prefab.name, report);
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs/UI/Item" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject item = Asset<GameObject>(owned, path); CheckPrefab(item, path, report);
                    if (item.GetComponent<DemoDamageTextItem>() != null)
                        Require(item == config.DamageTextPrefab.gameObject, "配置中的飘字并非UI/Item里的交付模板。");
                }
                foreach (string field in new[] { "menuSceneAsset", "gameSceneAsset" })
                {
                    string path = fields.FindProperty(field).stringValue; bool found = false;
                    foreach (AssetBundle bundle in owned.Values)
                        if (bundle.isStreamedSceneAssetBundle) foreach (string scene in bundle.GetAllScenePaths()) found |= string.Equals(path, scene, StringComparison.OrdinalIgnoreCase);
                    Require(found, "交付包场景缺失：" + path); report.checks.Add("scene=" + path);
                }
                string sourceRoot = Path.Combine(build, "ResourceBuild/Package"); string package = null; DateTime newest = DateTime.MinValue;
                foreach (string candidate in Directory.GetDirectories(sourceRoot, "Windows64", SearchOption.AllDirectories))
                {
                    string version = Path.Combine(candidate, "GameFrameworkVersion.dat");
                    if (File.Exists(version) && File.GetLastWriteTimeUtc(version) > newest) { newest = File.GetLastWriteTimeUtc(version); package = candidate; }
                }
                Require(package != null, "找不到生成的Package版本文件。");
                foreach (string file in Directory.GetFiles(package, "*.dat"))
                {
                    string name = Path.GetFileName(file); string hash = Hash(file);
                    Require(hash == Hash(Path.Combine(Application.streamingAssetsPath, name)) && hash == Hash(Path.Combine(delivered, name)), "Package/Assets/交付目录字节不一致：" + name);
                    report.files.Add(new FileEvidence { name = name, sha256 = hash });
                }
                Require(report.files.Count == names.Length + 1, "资源包或版本文件数量不完整。");
                report.checks.Add("allPackageCopiesByteEqual=true"); report.result = "Passed";
                Debug.Log("[Demo.PackedAssetQA] PASS 交付资源包实际加载、UI/实体/特效和全部UI Item脚本/字体/引用、4类实体贴图与受击Shader、菜单/玩法场景及3处资源字节一致。");
            }
            catch (Exception error) { report.error = error.ToString(); throw; }
            finally
            {
                foreach (AssetBundle bundle in owned.Values) if (bundle != null) bundle.Unload(true);
                if (manifestBundle != null) manifestBundle.Unload(true);
                string path = Path.Combine(project, "openspec/changes/refactor-demo-runtime-presentation/packed-asset-results.json");
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
            }
        }
        private static void Load(string name, AssetBundleManifest manifest, Dictionary<string, string> files, Dictionary<string, ResourceInfo> infos, Dictionary<string, AssetBundle> owned, HashSet<string> visiting)
        {
            if (owned.ContainsKey(name)) return;
            Require(visiting.Add(name), "包依赖出现环：" + name);
            foreach (string dependency in manifest.GetDirectDependencies(name)) Load(dependency, manifest, files, infos, owned, visiting);
            Require(files.TryGetValue(name, out string path), "交付目录缺少资源包：" + name);
            Require(infos.TryGetValue(name, out ResourceInfo info), "构建报告缺少包加载类型：" + name);
            byte[] bytes = File.ReadAllBytes(path);
            // 与GF ResourceLoader.DefaultDecryptResourceCallback一致，不改变交付文件。
            if (info.loadType == 2) GameFramework.Utility.Encryption.GetQuickSelfXorBytes(bytes, GameFramework.Utility.Converter.GetBytes(info.crc));
            else if (info.loadType == 3) GameFramework.Utility.Encryption.GetSelfXorBytes(bytes, GameFramework.Utility.Converter.GetBytes(info.crc));
            else Require(info.loadType == 0 || info.loadType == 1, "不支持的AssetBundle加载类型：" + info.loadType);
            Require(bytes.Length == info.length && GameFramework.Utility.Verifier.GetCrc32(bytes) == info.crc, "交付资源解密后的长度/CRC不匹配：" + name);
            AssetBundle bundle = AssetBundle.LoadFromMemory(bytes); Require(bundle != null, "交付资源包不能加载：" + name);
            owned.Add(name, bundle); visiting.Remove(name);
        }
        private static Dictionary<string, ResourceInfo> ResourceInfos(string build)
        {
            string[] reports = Directory.GetFiles(Path.Combine(build, "ResourceBuild/BuildReport"), "BuildReport.xml", SearchOption.AllDirectories);
            Array.Sort(reports, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
            Require(reports.Length > 0, "生成目录缺少构建报告。");
            var xml = new XmlDocument(); xml.Load(reports[0]);
            var result = new Dictionary<string, ResourceInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlElement resource in xml.SelectNodes("/UnityGameFramework/BuildReport/Resources/Resource"))
            {
                var windows = (XmlElement)resource.SelectSingleNode("Codes/Windows64");
                Require(windows != null, "资源构建报告缺少Windows64：" + resource.GetAttribute("Name"));
                result.Add(resource.GetAttribute("Name"), new ResourceInfo { loadType = int.Parse(resource.GetAttribute("LoadType")),
                    length = int.Parse(windows.GetAttribute("Length")), crc = int.Parse(windows.GetAttribute("HashCode")) });
            }
            return result;
        }
        private static T Asset<T>(Dictionary<string, AssetBundle> bundles, string path) where T : UnityEngine.Object
        {
            foreach (AssetBundle bundle in bundles.Values)
            {
                if (bundle.isStreamedSceneAssetBundle || !bundle.Contains(path)) continue;
                T asset = bundle.LoadAsset<T>(path); Require(asset != null, "包中类型不匹配或资源无法解析：" + path); return asset;
            }
            throw new InvalidOperationException("交付资源包中未找到：" + path);
        }
        private static void CheckPrefab(GameObject prefab, string path, Report report)
        {
            Require(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(prefab)), "检查到了源Prefab而非交付包：" + path);
            foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                Require(component != null, "包内Missing Script：" + path);
                Require(component.GetType().Namespace != "Shanhai.Demo.QA", "交付Prefab仍挂载临时QA组件：" + path);
                var iterator = new SerializedObject(component).GetIterator();
                while (iterator.Next(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                        Require(iterator.objectReferenceValue != null || iterator.objectReferenceInstanceIDValue == 0, "包内引用丢失：" + path + "/" + iterator.propertyPath);
            }
            foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
                Require(text.font != null && text.font.material != null && text.font.atlasTexture != null, "包内字体/材质/图集缺失：" + path);
            report.checks.Add("prefab=" + path);
        }
        private static string Hash(string path)
        {
            using (var hash = SHA256.Create()) using (var file = File.OpenRead(path))
                return BitConverter.ToString(hash.ComputeHash(file)).Replace("-", string.Empty).ToLowerInvariant();
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
