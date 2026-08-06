#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

public static class AssetRefSearchTool
{
    static string[] assetGUIDs;
    static string[] assetPaths;
    static string[] allAssetPaths;
    static Thread thread;

    [MenuItem("Assets/查找/查找资源引用(非主线程AssetDatabase查找)", false)]
    static void FindAssetRefMenu()
    {
        if (Selection.assetGUIDs.Length == 0)
        {
            Debug.Log("请先选择任意一个组件，再击此菜单");
            return;
        }

        assetGUIDs = Selection.assetGUIDs;

        assetPaths = new string[assetGUIDs.Length];

        for (int i = 0; i < assetGUIDs.Length; i++)
        {
            assetPaths[i] = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
        }

        allAssetPaths = AssetDatabase.GetAllAssetPaths();

        thread = new Thread(new ThreadStart(FindAssetRef));
        thread.Start();
    }

    static void FindAssetRef()
    {
        Debug.Log(string.Format("开始查找引用{0}的资源。", string.Join(",", assetPaths)));
        List<string> logInfo = new List<string>();
        string path;
        string log;
        for (int i = 0; i < allAssetPaths.Length; i++)
        {
            path = allAssetPaths[i];
            if (path.EndsWith(".prefab") || path.EndsWith(".unity"))
            {
                string content = File.ReadAllText(path);
                if (content == null)
                {
                    continue;
                }

                for (int j = 0; j < assetGUIDs.Length; j++)
                {
                    if (content.IndexOf(assetGUIDs[j]) > 0)
                    {
                        log = string.Format("{0} 引用了 {1}", path, assetPaths[j]);
                        logInfo.Add(log);
                    }
                }
            }
        }

        for (int i = 0; i < logInfo.Count; i++)
        {
            Debug.Log(logInfo[i]);
        }

        Debug.Log("选择对象引用数量：" + logInfo.Count);

        Debug.Log("查找完成");
    }
}


public class SearchWindows : EditorWindow
{
    [MenuItem("Assets/查找/查找资源引用(ripgrep查找)", false)]
    public static void OpenWindow()
    {
        (EditorWindow.GetWindow(typeof(SearchWindows)) as SearchWindows).Search = Selection.activeObject;
    }

    public Object Search;
    List<Object> SearchOut = new List<Object>();

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Search:", EditorStyles.boldLabel);
        Search = EditorGUILayout.ObjectField(Search, typeof(Object), true);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Search!"))
        {
            if (Search != null)
            {
                SearchOut.Clear();
                List<string> @out = new List<string>();
                string path = AssetDatabase.GetAssetPath(Search);
                if (!string.IsNullOrEmpty(path))
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    string meta = AssetDatabase.GetTextMetaFilePathFromAssetPath(path);
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.WorkingDirectory = Application.dataPath;
                    p.StartInfo.FileName = Application.dataPath.Replace("Unity/Assets", "Tools/ripgrep/rg.exe");
                    p.StartInfo.Arguments = $"-l {guid}";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    while (!p.StandardOutput.EndOfStream)
                    {
                        string line = $"Assets/{p.StandardOutput.ReadLine().Replace("\\", "/")}";
                        if (line != meta)
                        {
                            var item = AssetDatabase.LoadAssetAtPath(line, typeof(Object));
                            if (item != null)
                                SearchOut.Add(item);
                        }
                    }
                }
            }
        }

        if (SearchOut.Count > 0)
        {
            GUILayout.Label("Out:", EditorStyles.boldLabel);
            foreach (var o in SearchOut)
            {
                EditorGUILayout.ObjectField(o, typeof(Object), true);
            }
        }
    }
}
#endif