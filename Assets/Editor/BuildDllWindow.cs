using System;
using System.IO;
using HybridCLR.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using HybridCLR.Editor.Commands;
using YooAsset;
using YooAsset.Editor;

/// <summary>
/// 热更一键打包构建窗口（基于 UI Toolkit 构建）
/// 核心流程：HybridCLR编译DLL -> 拷贝并伪装为.bytes -> YooAsset打包AB -> 拷贝至外部发布包目录
/// </summary>
public class BuildDllWindow : EditorWindow
{
    [MenuItem("Tools/构建dll")]
    public static void ShowWindow()
    {
        var window = GetWindow<BuildDllWindow>();
        window.titleContent = new GUIContent("热更构建");
        window.minSize = new Vector2(300, 200);
    }

    // 用于在 EditorPrefs 中持久化保存输出路径的 Key
    private const string OutPutKey = "BuildDllWindow_OutPutKey";

    // 缓存当前用户选择的外部 PC 端包体路径
    private string _outPutPath = "";

    /// <summary>
    /// UI Toolkit 界面渲染入口
    /// </summary>
    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        // 1. 顶部大标题
        Label titleLabel = new Label("热更一键构建流程")
        {
            style =
            {
                fontSize = 20,
                unityFontStyleAndWeight = FontStyle.Bold,
                marginTop = 20,
                marginBottom = 20,
                alignSelf = Align.Center
            }
        };
        root.Add(titleLabel);

        // 2. 内部打包按钮（仅编译DLL + 打AB包到项目中）
        Button buildSABtn = new Button
        {
            text = "执行构建StreamingAssets",
            style =
            {
                height = 40,
                fontSize = 25,
                marginLeft = 20,
                marginRight = 20,
                backgroundColor = new StyleColor(Color.yellow),
                color = Color.black
            }
        };
        buildSABtn.clicked += () => { OnBuildASButtonClicked(); };
        root.Add(buildSABtn);

        // 3. 外部路径选择区域（横向容器）
        VisualElement pathContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                marginLeft = 20,
                marginRight = 20,
                marginBottom = 20
            }
        };

        // 路径输入框（读取本地缓存）
        _outPutPath = EditorPrefs.GetString(OutPutKey);
        TextField pathTextField = new TextField("OutPut路径:")
        {
            value = _outPutPath,
            style = { flexGrow = 1 }
        };
        pathTextField.RegisterValueChangedCallback(evt =>
        {
            _outPutPath = evt.newValue;
            EditorPrefs.SetString(OutPutKey, _outPutPath); // 手动修改时同步保存
        });
        pathContainer.Add(pathTextField);

        // 路径浏览按钮
        Button browseBtn = new Button(() =>
        {
            // 弹出系统原生的文件夹选择面板
            string selectedPath = EditorUtility.OpenFolderPanel("选择OutPut目录", _outPutPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _outPutPath = selectedPath;
                pathTextField.value = _outPutPath;
                EditorPrefs.SetString(OutPutKey, _outPutPath); // 选择后同步保存
            }
        })
        {
            text = "浏览..."
        };
        pathContainer.Add(browseBtn);
        root.Add(pathContainer);

        // 4. 一键执行按钮（内部打包 + 拷贝到外部）
        Button buildBtn = new Button
        {
            text = "一键执行构建流程",
            style =
            {
                height = 40,
                fontSize = 25,
                marginLeft = 20,
                marginRight = 20,
                backgroundColor = new StyleColor(Color.green),
                color = Color.black
            }
        };
        buildBtn.clicked += OnBuildAllButtonClicked;
        root.Add(buildBtn);
    }

    /// <summary>
    /// 【核心构建流 1】：编译 DLL -> 拷贝重命名 -> YooAsset 打包
    /// </summary>
    private bool OnBuildASButtonClicked()
    {
        Debug.Log("========== 开始构建StreamingAssets ==========");

        // [步骤 1] 触发 HybridCLR 编译当前平台的代码 DLL
        Debug.Log("【1】开始执行 HybridCLR: CompileDllActiveBuildTarget...");
        CompileDllCommand.CompileDllActiveBuildTarget();

        // 确定项目中存放热更 DLL 的目录：Assets/Bundle/Codes
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string targetDir = Path.Combine(projectRoot, "Assets", "Bundle", "Codes");
        // 目标文件需加上 .bytes 后缀，让 Unity 将其识别为 TextAsset
        string targetFilePath = $"{targetDir}/HotUpdate.dll.bytes";

        // [步骤 2] 删除项目中旧的 DLL.bytes
        Debug.Log($"【2】尝试删除旧文件: {targetFilePath}");
        if (File.Exists(targetFilePath))
        {
            AssetDatabase.DeleteAsset(targetFilePath); // 注意：DeleteAsset理应传相对路径(Assets/...)，这里传绝对路径存在隐患
            Debug.Log("旧的 HotUpdate.dll.bytes 已删除。");
        }

        // [步骤 3] 从 HybridCLR 产出目录中，提取刚编译好的原始 HotUpdate.dll
        string hotUpdateDllDir =
            SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget);
        string sourceDllPath = Path.Combine(hotUpdateDllDir, "HotUpdate.dll");

        Debug.Log($"【3】准备拷贝新文件，源路径: {sourceDllPath}");
        if (!File.Exists(sourceDllPath))
        {
            Debug.LogError($"未找到生成的 HotUpdate.dll，路径: {sourceDllPath}。构建终止！");
            return false;
        }

        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        // 拷贝新 DLL 并重命名为 .bytes 覆盖至项目中
        File.Copy(sourceDllPath, targetFilePath, true);
        // 强制刷新 AssetDatabase，生成对应的 .meta 文件，否则 YooAsset 无法识别该资源
        AssetDatabase.Refresh();
        Debug.Log($"拷贝完成并已生成 meta 文件: {targetFilePath}");

        // [步骤 4] 启动 YooAsset 打包管线，将该 DLL.bytes 及其他资源打成 AssetBundle
        Debug.Log("【4】开始运行 YooAsset AssetBundle Builder...");
        if (!RunYooAssetBuilds())
            return false;

        Debug.Log("========== 构建StreamingAssets执行完毕 ==========");
        return true;
    }

    private bool RunYooAssetBuilds()
    {
        if (!RunYooAssetBuild("DefaultPackage"))
            return false;

        if (!RunYooAssetBuild("ConfigPackage", true))
            return false;

        if (!RunYooAssetBuild("CodesPackage", true))
            return false;

        return true;
    }

    /// <summary>
    /// 执行 YooAsset 的构建打包，根据包名动态选择管线
    /// </summary>
    private bool RunYooAssetBuild(string packageName, bool isBBP = false)
    {
        string outputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot(); // 默认输出根目录
        string pipelineName = AssetBundleBuilderSetting.GetPackageBuildPipeline(packageName); // 获取当前配置的构建管线

        BuildParameters buildParameters;
        if (isBBP)
        {
            buildParameters = new BuiltinBuildParameters
            {
                BuildOutputRoot = outputRoot,
                BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot(),
                BuildPipeline = pipelineName,
                BuildBundleType = (int)EBuildBundleType.AssetBundle,
                BuildTarget = EditorUserBuildSettings.activeBuildTarget,
                PackageName = packageName,
                PackageVersion = DateTime.Now.ToString("yyyy-MM-dd-HHmm"),
                EnableSharePackRule = true,
                VerifyBuildingResult = true,
                FileNameStyle = EFileNameStyle.HashName,
                BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll,
                BuildinFileCopyParams = string.Empty,
                CompressOption = ECompressOption.LZ4,
            };
        }
        else
        {
            buildParameters = new ScriptableBuildParameters
            {
                BuildOutputRoot = outputRoot,
                BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot(),
                BuildPipeline = pipelineName,
                BuildBundleType = (int)EBuildBundleType.AssetBundle,
                BuildTarget = EditorUserBuildSettings.activeBuildTarget,
                PackageName = packageName,
                PackageVersion = DateTime.Now.ToString("yyyy-MM-dd-HHmm"),
                EnableSharePackRule = true,
                VerifyBuildingResult = true,
                FileNameStyle = EFileNameStyle.HashName,
                BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll,
                BuildinFileCopyParams = string.Empty,
                CompressOption = ECompressOption.LZ4,
                ClearBuildCacheFiles = false, // 设为 false 以保留缓存
                UseAssetDependencyDB = false
            };
        }

        // 实例化构建管线并执行
        IBuildPipeline pipeline = isBBP ? new BuiltinBuildPipeline() : new ScriptableBuildPipeline();
        var buildResult = pipeline.Run(buildParameters, true);

        if (buildResult.Success)
        {
            Debug.Log(
                $"YooAsset {packageName} ({(isBBP ? "BBP" : "SBP")}) 构建成功！输出路径: {buildResult.OutputPackageDirectory}");
        }
        else
        {
            Debug.LogError($"YooAsset {packageName} 构建失败: {buildResult.ErrorInfo}");
            EditorUtility.DisplayDialog("构建失败", buildResult.ErrorInfo, "确定");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 【核心构建流 2】：执行内部打包 -> 将项目最新的 StreamingAssets 拷贝覆盖到外部发布包（如PC端目录）中
    /// </summary>
    private void OnBuildAllButtonClicked()
    {
        long startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 1. 先执行内部打包（编译DLL + 打AB包到工程目录）
        if (!OnBuildASButtonClicked())
            return;

        Debug.Log("========== 开始复制 StreamingAssets ==========");

        // 2. 校验外部发布包路径是否有效
        if (string.IsNullOrEmpty(_outPutPath))
        {
            Debug.LogError("output file path is null or empty");
            return;
        }

        // 3. 拼接外部包的内置数据目录（例如已打好的 PC 端包 NationalRacing_Data/StreamingAssets）
        string outputAS = Path.Combine(_outPutPath, "NationalRacing_Data", "StreamingAssets");
        outputAS = outputAS.Replace('\\', '/');

        // 如果外部包的数据目录都不存在，说明选错路径或者还未 Build Player
        if (!Directory.Exists(outputAS))
        {
            Debug.LogError("no Directory " + outputAS);
            return;
        }

        // 4. 清空外部包旧的资源文件夹
        Directory.Delete(outputAS, true);
        Debug.Log("output 旧的 StreamingAssets 已删除。");

        // 5. 将工程内刚打好的最新 StreamingAssets 文件夹完整拷贝过去
        FileUtil.CopyFileOrDirectory(Application.streamingAssetsPath, outputAS);

        // 拷贝完成后，尝试运行目标文件夹中的 exe 游戏程序
        string[] exeFiles = Directory.GetFiles(_outPutPath, "*.exe", SearchOption.TopDirectoryOnly);
        string targetExe = null;
        foreach (var exe in exeFiles)
        {
            if (!exe.EndsWith("UnityCrashHandler64.exe", StringComparison.OrdinalIgnoreCase) &&
                !exe.EndsWith("UnityCrashHandler32.exe", StringComparison.OrdinalIgnoreCase))
            {
                targetExe = exe;
                break;
            }
        }

        if (!string.IsNullOrEmpty(targetExe))
        {
            Debug.Log($"拷贝完成，启动游戏: {targetExe}");
            System.Diagnostics.Process.Start(targetExe);
        }
        else
        {
            Debug.LogWarning($"未找到 exe 文件，打开文件夹: {_outPutPath}");
            EditorUtility.RevealInFinder(_outPutPath);
        }

        Debug.Log("========== 复制 StreamingAssets 结束 ==========");

        // 统计总耗时
        long time = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - startTime;
        Debug.Log($"一键构建总耗时：{time / 60}分{time % 60}秒");
    }
}