using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;

#if UNITY_EDITOR

namespace XN.Tools
{
    [System.Serializable]
    public class BranchCommitPair
    {
        public string branchName;
        public string commitHash;
    }
    
    [FilePath("Assets/Script/Editor/ConfigImporterSettings.asset")]
    public class ConfigImporterSettings : ScriptableSingleton<ConfigImporterSettings>
    {
        [FolderPath(AbsolutePath = true)]
        public string svnPath;
        [FolderPath(AbsolutePath = true)]
        public string svnExePath;
        public List<BranchCommitPair> branchLastCommitList = new();

        public void RefreshSvnPath(string path)
        {
            svnPath = path;
            this.Save();
        }

        public void RefreshSvnExePath(string path)
        {
            svnExePath = path;
            this.Save();
        }

        // 更新除去的最近节点；用于给测试知道程序更新的日志。
        public void RefreshLastCommit(string branchName, string commitHash)
        {
            var pair = branchLastCommitList.Find(x => x.branchName == branchName);
            if (pair != null)
                pair.commitHash = commitHash;
            else
                branchLastCommitList.Add(new BranchCommitPair { branchName = branchName, commitHash = commitHash });
            this.Save();
        }
        public string GetLastCommitByBranchName(string branchName)
        {
            return branchLastCommitList.Find(x => x.branchName == branchName)?.commitHash;
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}

#endif