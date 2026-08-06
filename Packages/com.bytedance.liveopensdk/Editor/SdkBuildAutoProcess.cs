// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.IO;
using System.Threading.Tasks;
using ByteDance.LiveOpenSdk.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Editor
{
    public interface ICloudSyncChecker
    {
        bool IsUsingCloudSync();
    }

    public class SdkBuildAutoProcess : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => -1;

        private string SdkVersion => LiveOpenSdk.Instance.Version;

        // 打包前的回调检查
        // 用于存储外部接口的实例
        public static ICloudSyncChecker CloudSyncUsageChecker;

        private DanmuFileUtils DanmuFileUtils => DanmuFileUtils.Instance;

        public SdkBuildAutoProcess()
        {
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!CheckBeforeBuild())
            {
                throw new BuildFailedException("打包前检查不通过，打包流程被打断。");
            }
        }

        private bool CheckBeforeBuild()
        {
            var checker = CloudSyncUsageChecker;
            if (checker == null)
                Debug.LogError("云同步-构建检查 Error! Assert CloudSyncUsageChecker failed!");
            if (checker != null && checker.IsUsingCloudSync())
            {
                if (!DanmuFileUtils.IsConfigExist() || !DanmuFileUtils.LoadConfig().IsSuccess)
                {
                    ShowConfigWindow();
                    return false;
                }
            }

            return true;
        }

        private static async void ShowConfigWindow()
        {
            try
            {
                if (IsEditorBusy())
                {
                    Debug.LogError("云同步-构建检查 Error! " + SdkConfigWindow.UsageNoConfigWarning);
                    await Task.Delay(100);
                    while (IsEditorBusy())
                    {
                        await Task.Yield();
                    }
                }

                Debug.LogError("云同步-构建检查 Error! " + SdkConfigWindow.UsageNoConfigWarning);
                SdkConfigWindow.ShowWindow();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static bool IsEditorBusy() => EditorApplication.isCompiling || EditorApplication.isUpdating ||
                                              BuildPipeline.isBuildingPlayer;

        public void OnPostprocessBuild(BuildReport report)
        {
            var outputPath = report.summary.outputPath;
            var outputFolderPath = Path.GetDirectoryName(outputPath);
            Debug.Log($"{nameof(SdkBuildAutoProcess)} OnPostprocessBuild" +
                      $" sdk {SdkVersion} app {Application.version} {report.summary.platform}" +
                      $", outputPath: {outputPath}");

            if (SdkEditorTool.IsBuildForMobile())
            {
                Debug.Log($"{nameof(SdkBuildAutoProcess)} skip for mobile, {SdkEditorTool.ActiveBuildTarget}");
                return;
            }

            CopyTools(outputFolderPath);
            CopyConfig(outputFolderPath);

            Debug.Log($"{nameof(SdkBuildAutoProcess)} finish" +
                      $" sdk {SdkVersion} app {Application.version}");
        }

        private void CopyTools(string outputFolderPath)
        {
            var pkgPath = SdkEditorTool.SdkPackagePath;
            SdkEditorTool.CopyFile($"{pkgPath}/Plugins/parfait_crash_handler.exe", outputFolderPath);
        }

        private void CopyConfig(string outputFolderPath)
        {
            if (DanmuFileUtils.Instance.IsConfigExist())
            {
                SdkEditorTool.CopyFile(DanmuFileUtils.ConfigFilePath, outputFolderPath);
            }
        }
    }
}