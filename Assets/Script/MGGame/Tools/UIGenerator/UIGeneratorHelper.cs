#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XN
{
    public static class UIGeneratorHelper
    {
        /// <summary>
        /// 字符裁剪 
        /// </summary>
        public static string Between(string src, string findfrom, string findto, string defaultString)
        {
            int start = src.IndexOf(findfrom);
            int to = src.IndexOf(findto, start + findfrom.Length);
            if (start < 0 || to < 0) return defaultString;
            string s = src.Substring(start + findfrom.Length, to - start - findfrom.Length);
            return s;
        }
        
        /// <summary>
        /// 对生成的代码文件进行基础格式化清理。
        /// 主要用于移除因模板替换可能产生的过多连续空行，并强制刷新资源导入。
        /// </summary>
        /// <param name="filePath">代码文件绝对路径</param>
        public static void FormatScript(string filePath)
        {
            if (!File.Exists(filePath)) return;

            // 获取相对于 Assets 文件夹的局部路径
            string assetPath = "Assets" + filePath.Substring(Application.dataPath.Length);
            // 替换反斜杠为正斜杠，保证跨平台和 Unity API 路径兼容
            assetPath = assetPath.Replace("\\", "/");

            // 简单格式化：移除多余空行，确保大括号对齐
            string content = File.ReadAllText(filePath);
            
            // 简单的正则文本替换：将超过两个以上的连续换行符压缩为两个，保持代码整洁
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\n\s*\n\s*\n", "\n\n");
            
            // 写回文件
            File.WriteAllText(filePath, content);
            
            // 强制重新导入该脚本以应用修改
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
#endif