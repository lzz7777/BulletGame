//====================================================
//Author:lixin
//Time  :2026/1/27 18:05
//Desc  :
//====================================================

using YooAsset;

#if UNITY_EDITOR

namespace XN.Tools
{
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using cfg;
    using XN;
    using cfg.Global;
    using YooAsset.Editor;
    
    public class XnBuildExt : EditorWindow
    {
        private ChannelCmd _channel = ChannelCmd.DouYin;
        private bool _development;
        private string _productName;
        private string _outputFolder;
        private string _version = "1.0.0";
        private GameModel _gameModel = GameModel.Release;
        private bool _buildYooAsset = true;
        
        private DebugInitMode _debugInit = DebugInitMode.Off;

        private enum DebugInitMode
        {
            Off = 0,
            On = 1
        }
        
        [System.Serializable]
        private class ConstJsonDto
        {
            public int DebugInt;
            public string HostAddress;
            public ChannelCmd CurrChannel;
            public string Ver;
        }

        #region Editor

        public static void ShowWindow()
        {
            var win = GetWindow<XnBuildExt>("构建");
            win.minSize = new Vector2(380, 200);
            win.Show();
        }

        private void OnEnable()
        {
            _productName = PlayerSettings.productName;
            SyncProjectParameters();
            OnChanneBuildOutputFolder();
        }

        private void OnChanneBuildOutputFolder()
        {
            string folderName;
            if (_gameModel == GameModel.Debug)
            {
                switch (_channel)
                {
                    case ChannelCmd.DouYin:
                        folderName = $"Build/{_productName}_Test";
                        break;
                    case ChannelCmd.KuaiShou:
                        folderName = $"Build/{_productName}_Test";
                        break;
                    default:
                        Debug.LogError($"未处理渠道{_channel}");
                        folderName = $"Build/{_productName}_{_channel}";
                        break;
                }
            }
            else
            {
                switch (_channel)
                {
                    case ChannelCmd.KuaiShou:
                        folderName = $"Build/{_productName}";
                        break;
                    case ChannelCmd.DouYin:
                    default:
                        folderName = $"Build/{_productName}_{_version}";
                        break;
                }
                // Release 模式
            }

            _outputFolder = Path.Combine(Directory.GetCurrentDirectory(), folderName);
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("渠道", GUILayout.Width(60));
            var newChannel = (ChannelCmd)EditorGUILayout.EnumPopup(_channel, GUILayout.Width(120));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("模式", GUILayout.Width(60));
            var newGameModel = (GameModel)EditorGUILayout.EnumPopup(_gameModel, GUILayout.Width(120));
            GUILayout.Space(30);
            GUILayout.Label("是否开GM/Log", GUILayout.Width(60));
            EditorGUI.BeginDisabledGroup(newGameModel == GameModel.Release);
            var newDebugInit = (DebugInitMode)EditorGUILayout.EnumPopup(_debugInit, GUILayout.Width(120));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                _channel = newChannel;
                _gameModel = newGameModel;
                _debugInit = newDebugInit;
                OnChanneBuildOutputFolder();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("读取配置", GUILayout.Width(80))) SyncProjectParameters();
            if (GUILayout.Button("写入配置", GUILayout.Width(80))) WriteConstConfigJson(_gameModel, _debugInit, _channel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _version = EditorGUILayout.TextField("版本号", _version);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            _development = EditorGUILayout.Toggle("Development Build", _development);
            _buildYooAsset = EditorGUILayout.Toggle("Build YooAsset", _buildYooAsset);

            EditorGUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            var newProductName = EditorGUILayout.TextField("Product Name", _productName);
            if (EditorGUI.EndChangeCheck())
            {
                _productName = newProductName;
                PlayerSettings.productName = _productName;
                OnChanneBuildOutputFolder();
            }

            EditorGUILayout.BeginHorizontal();
            _outputFolder = EditorGUILayout.TextField("输出目录", _outputFolder);
            if (GUILayout.Button("选择目录", GUILayout.Width(80)))
            {
                var sel = EditorUtility.OpenFolderPanel("选择输出目录", _outputFolder, "");
                if (!string.IsNullOrEmpty(sel)) _outputFolder = sel;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            if (GUILayout.Button("应用参数 并开始构建"))
            {
                WriteConstConfigJson(_gameModel, _debugInit, _channel);
                ApplyGameModel(_gameModel);
                
                if (_buildYooAsset)
                {
                    if (BuildYooAsset())
                    {
                        Debug.Log($"AB okkkkkkkkk ---> Build APK");
                        BuildWindows64();
                    }
                }
                else
                {
                    BuildWindows64();
                }
            }

            if (GUILayout.Button("打包 ZIP"))
            {
                ZipOutputFolder();
            }
        }

        #endregion

        #region Data

        private static string ConstConfigPath =>
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Script", "ConfigExtend", "ConstConfigCategory.cs");

        private void WriteConstConfigJson(GameModel mode, DebugInitMode debugInit, ChannelCmd channel)
        {
            var path = GetConstJsonPath();
            
            var data = new ConstJsonDto
            {
                DebugInt = (mode == GameModel.Debug && debugInit == DebugInitMode.On) ? 1 : 0,
                HostAddress = ConstConfigCategory.DebugHost,
                CurrChannel = channel,
                Ver = _version
            };
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            FixMetaAndImport(path);

            // 修改场景，打开Game.unity 并 设置GameModel
            var sceneGUIDS = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            var gameScene = sceneGUIDS.Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => p.Contains("Game"));
            if (!string.IsNullOrEmpty(gameScene))
            {
                EditorSceneManager.OpenScene(gameScene);
                var mgr = FindObjectOfType<UIManager>(true);
                if (mgr != null)
                {
                    mgr.GameModel = mode;
                }
            }
        }

        private void SyncProjectParameters()
        {
            var path = GetConstJsonPath();
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var data = JsonUtility.FromJson<ConstJsonDto>(json);
                    if (data != null)
                    {
                        _channel = data.CurrChannel;
                        _debugInit = data.DebugInt == 1 ? DebugInitMode.On : DebugInitMode.Off;
                        if (!string.IsNullOrEmpty(data.Ver)) _version = data.Ver;
                    }
                }
                catch { }
            }

            var mgr = Object.FindObjectOfType<UIManager>(true);
            if (mgr == null)
            {
                var sceneGUIDS = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
                var gameScene = sceneGUIDS.Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => p.Contains("Game"));
                if (!string.IsNullOrEmpty(gameScene))
                {
                    EditorSceneManager.OpenScene(gameScene);
                    mgr = Object.FindObjectOfType<UIManager>(true);
                }
            }

            if (mgr != null)
            {
                _gameModel = mgr.GameModel;
            }

            OnChanneBuildOutputFolder();
        }

        private string GetConstJsonPath()
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "StreamingAssets");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var guids = AssetDatabase.FindAssets("ConstConfig t:TextAsset", new[] { "Assets/StreamingAssets" });
            if (guids != null && guids.Length > 0)
            {
                var found = AssetDatabase.GUIDToAssetPath(guids[0]);
                if (!string.IsNullOrEmpty(found)) return found;
            }

            return Path.Combine(dir, "ConstConfig.json");
        }

        private void FixMetaAndImport(string assetPath)
        {
            var metaPath = assetPath + ".meta";
            if (File.Exists(metaPath))
            {
                try
                {
                    var metaText = File.ReadAllText(metaPath);
                    if (string.IsNullOrEmpty(metaText) || !metaText.Contains("guid:"))
                    {
                        File.Delete(metaPath);
                    }
                }
                catch { }
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
        }

        private void ApplyGameModel(GameModel mode)
        {
            var mgr = Object.FindObjectOfType<UIManager>(true);
            if (mgr == null)
            {
                EditorUtility.DisplayDialog("未找到 UIManager", "请打开包含 UIManager 的 Game 场景后再应用 GameModel。", "确定");
                return;
            }
            mgr.GameModel = mode;
            EditorUtility.SetDirty(mgr);
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        #endregion

        #region Build / Zip

        private void BuildWindows64()
        {
            if (string.IsNullOrEmpty(_productName))
            {
                _productName = PlayerSettings.productName;
            } 

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("构建失败", "未在 Build Settings 中配置任何场景", "确定");
                return;
            }

            if (!Directory.Exists(_outputFolder)) Directory.CreateDirectory(_outputFolder);
            var exePath = Path.Combine(_outputFolder, _productName + ".exe");

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = _development ? BuildOptions.Development : BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("构建完成", $"输出路径:\n{exePath}", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("构建失败", report.summary.result.ToString(), "确定");
            }
        }
        private bool BuildYooAsset()
        {
            string outputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
            string pipelineName = AssetBundleBuilderSetting.GetPackageBuildPipeline("DefaultPackage");

            var buildParameters = new ScriptableBuildParameters();
            buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
            buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            buildParameters.BuildPipeline = pipelineName;
            buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle;
            buildParameters.BuildTarget = BuildTarget.StandaloneWindows64;
            buildParameters.PackageName = "DefaultPackage";
            buildParameters.PackageVersion = System.DateTime.Now.ToString("yyyy-MM-dd-HHmm");
            buildParameters.EnableSharePackRule = true;
            buildParameters.VerifyBuildingResult = true;
            buildParameters.FileNameStyle = EFileNameStyle.HashName;
            buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll;
            buildParameters.BuildinFileCopyParams = string.Empty;
            buildParameters.CompressOption = ECompressOption.LZ4;
            buildParameters.ClearBuildCacheFiles = false;
            buildParameters.UseAssetDependencyDB = false;

            ScriptableBuildPipeline pipeline = new ScriptableBuildPipeline();
            var buildResult = pipeline.Run(buildParameters, true);

            if (buildResult.Success)
            {
                Debug.Log($"YooAsset Build Success: {buildResult.OutputPackageDirectory}");
                EditorUtility.RevealInFinder(buildResult.OutputPackageDirectory);
                return true;
            }
            else
            {
                Debug.LogError($"YooAsset Build Failed: {buildResult.ErrorInfo}");
                EditorUtility.DisplayDialog("YooAsset Build Failed", buildResult.ErrorInfo, "OK");
                return false;
            }
        }
        private void ZipOutputFolder()
        {
            if (string.IsNullOrEmpty(_outputFolder))
            {
                EditorUtility.DisplayDialog("打包ZIP失败", "输出目录为空，请先构建项目。", "确定");
                return;
            }

            if (!Directory.Exists(_outputFolder))
            {
                EditorUtility.DisplayDialog("打包ZIP失败", "输出目录不存在。", "确定");
                return;
            }

            // 获取父目录
            var parentDir = Directory.GetParent(_outputFolder)?.FullName;
            if (string.IsNullOrEmpty(parentDir)) return;
            
            var folderName = Path.GetFileName(_outputFolder);
            var zipPath = Path.Combine(parentDir, $"{folderName}.zip");
            
            if (File.Exists(zipPath)) File.Delete(zipPath);

            // 删除不需要打包的目录、文件夹
            var backupDir = Path.Combine(_outputFolder, $"{_productName}_BackUpThisFolder_ButDontShipItWithYourGame");
            var DebugDir = Path.Combine(_outputFolder, $"{_productName}_BurstDebugInformation_DoNotShip");

            if (Directory.Exists(backupDir))
            {
                try { Directory.Delete(backupDir, true); } catch { }
            }
            if (Directory.Exists(DebugDir))
            {
                try { Directory.Delete(DebugDir, true); } catch { }
            }
            
            // 快手得使用它的exe打包，那就调起他的打包工具
            if(_channel == ChannelCmd.KuaiShou && _gameModel == GameModel.Release)
            {
                string ksZipTools = Path.Combine(Directory.GetCurrentDirectory(), "Build/KsZipTools");
                // 里面的ks_manifest.json ， 复制到输出目录，并且版本号修改为_version
                string ksManifestPath = Path.Combine(ksZipTools, "ks_manifest.json");
                if (File.Exists(ksManifestPath))
                {
                    string ksManifestContent = File.ReadAllText(ksManifestPath);
                    // 替换 ${version}
                    ksManifestContent = ksManifestContent.Replace("${version}", _version);
                    
                    // 强制正则替换 version 字段
                    ksManifestContent = System.Text.RegularExpressions.Regex.Replace(
                        ksManifestContent, 
                        "\"version\":\\s*\"[^\"]*\"", 
                        $"\"version\": \"{_version}\""
                    );

                    File.WriteAllText(Path.Combine(_outputFolder, "ks_manifest.json"), ksManifestContent);
                }
                // 使用快手专门的 GamePackerTool.exe
                string gamePackerTool = Path.Combine(ksZipTools, "GamePackerTool.exe");
                if (File.Exists(gamePackerTool))
                {
                    // 仅启动，不等待，不卡死 Unity 进程
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = gamePackerTool,
                        UseShellExecute = true, // 使用系统 Shell 启动，相当于双击运行
                        WorkingDirectory = ksZipTools // 设置工作目录为工具所在目录
                    });
                }
            }
            else
            {
                try
                {
                    // 获取所有文件，用于计算进度
                    var allFiles = Directory.GetFiles(_outputFolder, "*.*", SearchOption.AllDirectories);
                    var totalFiles = allFiles.Length;

                    using (var zipFileStream = new FileStream(zipPath, FileMode.Create))
                    {
                        using (var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
                        {
                            var baseDirName = Path.GetFileName(_outputFolder);
                            for (int i = 0; i < totalFiles; i++)
                            {
                                var filePath = allFiles[i];
                                var relativePath = filePath.Substring(_outputFolder.Length + 1);
                                // 保持包含顶层文件夹的结构
                                var entryName = Path.Combine(baseDirName, relativePath);
                                EditorUtility.DisplayProgressBar("打包 ZIP", $"正在压缩: {relativePath}", (float)i / totalFiles);
                                archive.CreateEntryFromFile(filePath, entryName, System.IO.Compression.CompressionLevel.Optimal);
                            }
                        }
                    }

                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("打包ZIP成功", $"ZIP路径:\n{zipPath}", "确定");
                    EditorUtility.RevealInFinder(zipPath);
                }
                catch (System.Exception e)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("打包ZIP失败", e.Message, "确定");
                }
            }
        }
        
        #endregion

    }
}

#endif
