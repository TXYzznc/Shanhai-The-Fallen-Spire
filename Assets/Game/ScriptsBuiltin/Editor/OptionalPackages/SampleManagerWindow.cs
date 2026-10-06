using UnityEditor;
using UnityEngine;

namespace AiFriendlyFrame.Editor.Samples
{
    internal sealed class SampleManagerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _message;
        private MessageType _messageType = MessageType.Info;

        [MenuItem("Tools/AI Friendly Frame/Samples", false, 1200)]
        private static void OpenWindow()
        {
            GetWindow<SampleManagerWindow>("AI Friendly Frame 示例").minSize = new Vector2(520f, 320f);
        }

        private void OnEnable()
        {
            _message = "示例包为可选内容；仅安装需要查看的包。";
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("可选示例包", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("示例源文件位于 Samples~，安装前不会被 Unity 导入。" +
                                    "管理器只会写入每个包清单中声明的路径。", MessageType.Info);
            EditorGUILayout.HelpBox("运行框架或依赖框架启动配置的示例时，请始终从 Assets/Game/Scene/Launch.unity 进入播放模式。" +
                                    "“打开”仅用于检查入口场景；基础 UI 等独立样例可直接预览，但不会经过框架启动流程。", MessageType.Info);

            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, _messageType);
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;
                var packages = SamplePackageManager.DiscoverPackages();
                if (packages.Count == 0)
                {
                    EditorGUILayout.HelpBox("在 Samples~/ 下未找到 sample.json 示例包。", MessageType.Warning);
                }

                foreach (SamplePackageInfo package in packages)
                {
                    DrawPackage(package);
                }
            }

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("刷新"))
                {
                    AssetDatabase.Refresh();
                    ShowMessage("已刷新示例包列表。", MessageType.Info);
                }

                if (GUILayout.Button("打开包源目录"))
                {
                    EditorUtility.RevealInFinder(System.IO.Path.Combine(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..")), SamplePackageManager.SamplesRootRelativePath));
                }
            }
        }

        private void DrawPackage(SamplePackageInfo package)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!package.IsValid)
                {
                    EditorGUILayout.LabelField(System.IO.Path.GetFileName(package.PackageDirectory), EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(package.Error, MessageType.Error);
                    return;
                }

                bool installed = SamplePackageManager.IsInstalled(package);
                if (SamplePackageManager.HasPendingAppConfigsProfileRecovery(package))
                {
                    EditorGUILayout.LabelField(package.Manifest.displayName, EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("检测到上一次 AppConfigs 配置档切换未完成。请先恢复安装前配置，再继续安装或更新。", MessageType.Warning);
                    if (GUILayout.Button("恢复安装前 AppConfigs"))
                    {
                        Execute((out string message) => SamplePackageManager.TryRecoverPendingAppConfigsProfile(package, out message));
                    }

                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(package.Manifest.displayName, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"v{package.Manifest.version}", GUILayout.Width(80f));
                    EditorGUILayout.LabelField(installed ? "已安装" : "未安装", GUILayout.Width(90f));
                }

                EditorGUILayout.LabelField("入口场景", package.Manifest.entryScene, EditorStyles.miniLabel);
                EditorGUILayout.LabelField("安装目录", package.Manifest.installRoot, EditorStyles.miniLabel);
                if (package.Manifest.appConfigsProfile != null)
                {
                    EditorGUILayout.HelpBox("此包安装时会切换完整 AppConfigs 配置档；卸载时恢复安装前配置。配置档激活期间不能与其他修改 AppConfigs 的示例共存。", MessageType.Info);
                }

                if (package.Manifest.addEntrySceneToBuildSettings)
                {
                    EditorGUILayout.HelpBox("此包安装时会将入口场景加入 Build Settings；卸载时恢复安装前的场景列表。", MessageType.Info);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (!installed && GUILayout.Button("安装"))
                    {
                        Execute((out string message) => SamplePackageManager.TryInstall(package, out message));
                    }

                    using (new EditorGUI.DisabledScope(!installed))
                    {
                        if (GUILayout.Button("打开"))
                        {
                            Execute((out string message) => SamplePackageManager.TryOpenEntryScene(package, out message));
                        }

                        if (GUILayout.Button("校验"))
                        {
                            bool valid = SamplePackageManager.TryValidate(package, out string message);
                            ShowMessage(message, valid ? MessageType.Info : MessageType.Warning);
                        }

                        if (GUILayout.Button("修复 / 重装"))
                        {
                            if (EditorUtility.DisplayDialog("修复示例", "已安装文件将被包源文件替换，请先备份本地修改。", "修复", "取消"))
                            {
                                Execute((out string message) => SamplePackageManager.TryRepair(package, out message));
                            }
                        }

                        if (GUILayout.Button("卸载"))
                        {
                            TryUninstall(package);
                        }
                    }

                    if (GUILayout.Button("打开源文件"))
                    {
                        SamplePackageManager.RevealPackage(package);
                    }
                }
            }
        }

        private void TryUninstall(SamplePackageInfo package)
        {
            if (SamplePackageManager.TryValidate(package, out string validationMessage))
            {
                if (EditorUtility.DisplayDialog("卸载示例", $"仅删除“{package.Manifest.displayName}”记录的文件吗？", "卸载", "取消"))
                {
                    Execute((out string message) => SamplePackageManager.TryUninstall(package, false, out message));
                }

                return;
            }

            int choice = EditorUtility.DisplayDialogComplex("已修改的示例安装内容",
                $"{validationMessage}\n\n为保护本地修改，已阻止自动删除。",
                "打开已安装文件", "取消", "强制删除");
            if (choice == 0)
            {
                SamplePackageManager.RevealInstalledFiles(package);
            }
            else if (choice == 2 && EditorUtility.DisplayDialog("强制删除", "只会删除清单记录的文件，请先备份已修改内容。", "强制删除", "取消"))
            {
                Execute((out string message) => SamplePackageManager.TryUninstall(package, true, out message));
            }
        }

        private delegate bool SampleOperation(out string message);

        private void Execute(SampleOperation action)
        {
            if (action(out string message))
            {
                ShowMessage(message, MessageType.Info);
                Repaint();
                return;
            }

            ShowMessage(message, MessageType.Error);
        }

        private void ShowMessage(string message, MessageType type)
        {
            _message = message;
            _messageType = type;
            Repaint();
        }
    }
}
