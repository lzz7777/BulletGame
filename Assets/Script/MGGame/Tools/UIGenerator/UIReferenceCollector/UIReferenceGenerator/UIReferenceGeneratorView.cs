#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace XN
{
    public static class UIReferenceGeneratorView
    {
        // 目标生成路径（UI 目录）
        private static string UIPath = Application.dataPath + "\\Script\\MGGame\\UI";

        // 代码模板文件所在路径
        private static string TemplatePath = Application.dataPath +
                                             "\\Script\\MGGame\\Tools\\UIGenerator\\UIGeneratorTemplate\\UIGeneratorView";

        public static void Generate(UIReferenceCollector collector)
        {
            string className = collector.transform.name;
            string directory = UIPath + $"\\{className}";

            // 1. 生成View脚本（包含UI元素引用）
            GenerateViewScript(directory, className, collector);

            // 2. 生成Logic脚本（如果不存在）
            GenerateViewSystemScript(directory, className + "System");

            // 刷新 AssetDatabase，使生成的文件被 Unity 识别
            AssetDatabase.Refresh();

            // 3. 对新生成或修改过的文件执行简单的代码格式化
            UIGeneratorHelper.FormatScript(Path.Combine(directory, className + ".cs"));
        }

        /// <summary>
        /// 核心：生成 View 主面板绑定脚本。
        /// </summary>
        private static void GenerateViewScript(string directory, string className, UIReferenceCollector collector)
        {
            string filePath = Path.Combine(directory, className + ".cs");
            string insertFieldString = "\n "; // CustomFields 默认预留空行
            // 默认使用的库
            string usingsString =
                "using System.Collections.Generic;\nusing TMPro;\nusing UnityEngine;\nusing UnityEngine.UI;"; // 默认引用库

            // 如果文件已存在，则进行无损更新分析
            if (File.Exists(filePath))
            {
                // 获取原始代码内容
                string originScript = File.ReadAllText(filePath);

                // 提取 #region CustomFields 和 #endregion 之间的手写代码块
                insertFieldString = "";
                insertFieldString =
                    UIGeneratorHelper.Between(originScript, "#region CustomFields", "#endregion", insertFieldString);

                // 提取文件顶部的 using 声明区域（保留新增库）
                int namespaceIndex = originScript.IndexOf("namespace XN");
                if (namespaceIndex != -1)
                {
                    usingsString = originScript.Substring(0, namespaceIndex).Trim();
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // 开始遍历所有层级的子节点，搜集待绑定的 UI 元素
            StringBuilder fieldsBuilder = new StringBuilder();
            foreach (var objectData in collector.objectDatas)
            {
                foreach (var componentData in objectData.componentDatas)
                {
                    fieldsBuilder.AppendLine(
                        $"\t\tpublic {UICollectorData.GetComponentEntityType(componentData.ComponentEnum)} {componentData.name};");
                }
            }

            // 整理生成的字段列表
            string fields = fieldsBuilder.ToString().TrimEnd();

            // 读取代码模板并执行宏替换
            string templatePath = Path.Combine(TemplatePath, "UIGeneratorViewTemplate.txt");
            string content = File.ReadAllText(templatePath);
            content = content.Replace("#USING_NAMESPACES#", usingsString)
                .Replace("#CLASS_NAME#", className)
                .Replace("#FIELDS#", fields)
                .Replace("#CUSTOM_FIELDS#", insertFieldString);

            File.WriteAllText(filePath, content);

            UnityEngine.Debug.Log($"生成主视图绑定文件 {filePath} 成功");
        }

        /// <summary>
        /// 生成对应的 ViewSystem 静态扩展类。
        /// 仅在文件不存在时生成，避免覆盖手写逻辑。
        /// </summary>
        private static void GenerateViewSystemScript(string directory, string className)
        {
            string filePath = Path.Combine(directory, className + ".cs");
            if (File.Exists(filePath)) return; // 不覆盖已有逻辑文件

            // 剥离 "System" 后缀获取原始的 View 类名
            string viewName = className.Substring(0, className.Length - 6);

            // 读取逻辑类模板并执行宏替换
            string templatePath = Path.Combine(TemplatePath, "UIGeneratorViewSystemTemplate.txt");
            string content = File.ReadAllText(templatePath);

            content = content.Replace("#CLASS_NAME#", className)
                .Replace("#VIEW_NAME#", viewName);

            File.WriteAllText(filePath, content);

            UnityEngine.Debug.Log($"生成主视图系统逻辑文件 {filePath} 成功");
        }
    }
}
#endif