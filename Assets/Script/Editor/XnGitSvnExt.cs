//====================================================
//Author:lixin
//Time  :2025/12/11 10:26
//Desc  :
//====================================================

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace XN.Tools
{
    public class WeChatRobotResponse
    {
        public int errcode;
        public string errmsg;
    }

    public class XnGitSvnExt : OdinEditorWindow
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            svnPath = ConfigImporterSettings.Inst.svnPath;
            svnExePath = ConfigImporterSettings.Inst.svnExePath;
        }

        // [MenuItem("日志")]
        public static void DumpGitSvn()
        {
            XnGitSvnExt window = GetWindow<XnGitSvnExt>();
            window.titleContent = new GUIContent("日志");
            window.minSize = new Vector2(400, 100);
        }


        [Title("SVN")] [LabelText("SVN项目路径")] [FolderPath(AbsolutePath = true)] [OnValueChanged("OnSvnPathChanged")]
        public string svnPath;

        [LabelText("svn.exe的安装bin目录")] [FolderPath(AbsolutePath = true)] [OnValueChanged("OnSvnExePathChanged")]
        public string svnExePath;

        [ValueDropdown(nameof(GetRecentSvnRevisions))]
        public string lastSvnRevision;

        [LabelText("SVN子项目")] [ValueDropdown(nameof(GetSvnSubProjects))]
        public string svnSubProject = "配置目录";

        #region SVN提交记录

        public void OnSvnExePathChanged() => ConfigImporterSettings.Inst.RefreshSvnExePath(svnExePath);
        public void OnSvnPathChanged() => ConfigImporterSettings.Inst.RefreshSvnPath(svnPath);

        [HorizontalGroup("SVNButtons")]
        [Button("svn更新描述推送", ButtonSizes.Large)]
        public void CollectSvnDesc() => SvnUpdatePush();

        [HorizontalGroup("SVNButtons")]
        [Button("SVN拉+更", ButtonSizes.Large)]
        public void RunSvnBat() => RunSVNUpdateAndBat();

        public void SvnUpdatePush()
        {
            var rev = GetLatestSvnRevision();
            // if (this.lastSvnRevision == rev) return;

            // 打印
            string message = PrintSvnLogSimple(lastSvnRevision, rev);
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogError("无内容更新");
                return;
            }

            Debug.Log(message);
            PushWechatRobot(message);
            ConfigImporterSettings.Inst.RefreshLastCommit("svn", rev);
            lastSvnRevision = rev;
        }

        /// <summary>
        /// 拉取上条记录
        /// </summary>
        /// <returns></returns>
        public string GetLatestSvnRevision()
        {
            string output = RunSvnCommand("log -l 1 --xml");
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(output);
            var entry = doc.SelectSingleNode("/log/logentry");
            if (entry != null)
            {
                string rev = entry.Attributes["revision"].Value;
                return rev;
            }

            return null;
        }

        /// <summary>
        /// 标准svn信息获取
        /// </summary>
        /// <param name="args">指定命令行 拉取等</param>
        /// <returns></returns>
        public string RunSvnCommand(string args)
        {
            string svnExePath = ConfigImporterSettings.Inst.svnExePath + @"\\svn.exe";
            string workingPath =
                Path.GetFullPath(svnPath + "/策划/" + (string.IsNullOrEmpty(svnSubProject) ? "配置目录" : svnSubProject));
            using Process svn = new();
            svn.StartInfo = new()
            {
                FileName = svnExePath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                WorkingDirectory = workingPath
            };
            svn.Start();
            string output = svn.StandardOutput.ReadToEnd();
            string error = svn.StandardError.ReadToEnd();
            svn.WaitForExit();
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("SVN Error:\n" + error);
            }

            return output;
        }

        private List<ValueDropdownItem<string>> GetBatFiles()
        {
            List<ValueDropdownItem<string>> items = new();
            try
            {
                var basePath = string.IsNullOrEmpty(ConfigImporterSettings.Inst.svnPath)
                    ? svnPath
                    : ConfigImporterSettings.Inst.svnPath;
                if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath)) return items;
                var files = Directory.GetFiles(basePath, "*.bat", SearchOption.TopDirectoryOnly);
                foreach (var f in files)
                {
                    items.Add(new ValueDropdownItem<string>(Path.GetFileName(f), f));
                }
            }
            catch (Exception e)
            {
                Debug.LogError("枚举.bat失败: " + e.Message);
            }

            return items;
        }

        private List<ValueDropdownItem<string>> GetSvnSubProjects()
        {
            List<ValueDropdownItem<string>> items = new();
            items.Add(new ValueDropdownItem<string>("配置目录", "配置目录"));
            items.Add(new ValueDropdownItem<string>("配置目录MSYQ", "配置目录MSYQ"));
            try
            {
                var basePath = string.IsNullOrEmpty(ConfigImporterSettings.Inst.svnPath)
                    ? svnPath
                    : ConfigImporterSettings.Inst.svnPath;
                var planDir = Path.GetFullPath(basePath + "/策划/");
                if (Directory.Exists(planDir))
                {
                    var dirs = Directory.GetDirectories(planDir);
                    foreach (var d in dirs)
                    {
                        var name = Path.GetFileName(d);
                        if (!items.Any(i => i.Value == name))
                            items.Add(new ValueDropdownItem<string>(name, name));
                    }
                }
            }
            catch
            {
            }

            return items;
        }

        /// <summary>
        /// 拉取最近N条svn记录
        /// </summary>
        /// <returns></returns>
        public List<ValueDropdownItem<string>> GetRecentSvnRevisions()
        {
            if (string.IsNullOrEmpty(ConfigImporterSettings.Inst.svnExePath))
                return new List<ValueDropdownItem<string>>();
            string output = RunSvnCommand("log -l 30 --xml");
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(output);
            var entries = doc.SelectNodes("/log/logentry");
            List<ValueDropdownItem<string>> items = new();
            foreach (System.Xml.XmlNode entry in entries)
            {
                string rev = entry.Attributes["revision"].Value;
                string author = entry["author"]?.InnerText ?? "";
                string date = ToBeijingTime(entry["date"]?.InnerText ?? "");
                string msg = entry["msg"]?.InnerText.Replace("\n", " ").Replace("\r", " ") ?? "";
                string label = $"r{rev} | {date} | {author} | {msg}";
                items.Add(new ValueDropdownItem<string>(label, rev));
            }

            return items;
        }

        public string PrintSvnLogSimple(string startRev, string endRev)
        {
            string output = RunSvnCommand($"log -r {startRev}:{endRev} --xml");
            // 解析xml
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(output);
            var logentries = doc.SelectNodes("/log/logentry");
            if (logentries == null || logentries.Count == 0)
                return "";
            StringBuilder sb = new();
            string startDate = ToBeijingTime(logentries[0]["date"]?.InnerText ?? "");
            string endate = ToBeijingTime(logentries[^1]["date"]?.InnerText ?? "");
            string projectLabel = string.IsNullOrEmpty(svnSubProject) ? "未指定子目录" : svnSubProject;
            sb.AppendLine($"SVN - {projectLabel}");
            sb.AppendLine($"【{startDate} ~ {endate}】");
            foreach (System.Xml.XmlNode entry in logentries)
            {
                string rev = entry.Attributes["revision"].Value;
                string author = entry["author"]?.InnerText ?? "";
                string msg = entry["msg"]?.InnerText.Replace("\n", " ").Replace("\r", " ") ?? "";
                sb.AppendLine($"r{rev} | {author} | {msg}");
            }

            return sb.ToString();
        }

        // 工具函数
        private static string ToBeijingTime(string utcTime)
        {
            if (string.IsNullOrEmpty(utcTime)) return "";
            if (DateTime.TryParse(utcTime, out DateTime dtUtc))
            {
                var dtBj = dtUtc.ToUniversalTime().AddHours(8);
                return dtBj.ToString("yyyy-MM-dd HH:mm:ss");
            }

            return utcTime;
        }

        private void RunSVNUpdateAndBat()
        {
            // 1. 计算 Excel 目录的绝对路径
            string svnDir = Path.GetFullPath(ConfigImporterSettings.Inst.svnPath);
            string excelDir = Path.GetFullPath(ConfigImporterSettings.Inst.svnPath + "/策划/" +
                                               (string.IsNullOrEmpty(svnSubProject) ? "配置目录" : svnSubProject));

            // 2. SVN update
            if (!Directory.Exists(excelDir))
            {
                Debug.LogError("Excel 目录不存在: " + excelDir);
                return;
            }

            Process svn = new();
            svn.StartInfo.FileName = svnExePath + @"\\svn.exe"; // 需要svn(小乌龟）装命令行，才会有svn.exe
            svn.StartInfo.Arguments = "update";
            svn.StartInfo.WorkingDirectory = svnDir;
            svn.StartInfo.CreateNoWindow = true;
            svn.StartInfo.UseShellExecute = false;
            svn.StartInfo.RedirectStandardOutput = true;
            svn.StartInfo.RedirectStandardError = true;
            svn.Start();

            // 转码
            string error = "";
            using (var reader = new StreamReader(svn.StandardError.BaseStream, Encoding.GetEncoding("GB2312")))
            {
                error = reader.ReadToEnd();
            }

            string output;
            using (var reader = new StreamReader(svn.StandardOutput.BaseStream, Encoding.GetEncoding("GB2312")))
            {
                output = reader.ReadToEnd();
            }

            svn.WaitForExit();
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("SVN Error:\n" + error);
                return;
            }

            Debug.Log("SVN 拉取如下==========>" + output);
            // 3. 执行导出配置.bat
            string batPath = excelDir + "/gen生成[策划].bat";
            if (!File.Exists(batPath))
            {
                Debug.LogError("找不到 " + batPath);
                return;
            }

            Process bat = new();
            bat.StartInfo.FileName = batPath;
            bat.StartInfo.WorkingDirectory = excelDir;
            bat.StartInfo.CreateNoWindow = false;
            bat.StartInfo.UseShellExecute = true;
            bat.Start();

            Debug.Log("已执行 gen生成[策划].bat");
        }

        #endregion

        #region Git提交记录

        private static readonly string[] FilterKeywords = { "Merge branch" };
        private const string GitIgnoreStr = "https://e.coding.net/xuanniao1/danmuyouxi/BulletGame";

        [FormerlySerializedAs("StartCommit")]
        [Title("Git提交选择（区间）")]
        [ValueDropdown(nameof(GetStartCommitList), IsUniqueList = true)]
        public string startCommit;

        [FormerlySerializedAs("EndCommit")] [ValueDropdown(nameof(GetEndCommitList), IsUniqueList = true)]
        public string endCommit;

        // 拉取 start 和 end 之间的 commit（包含头尾）
        private List<CommitEntry> GetCommitsBetween(string start, string end)
        {
            string cmd =
                $"log {start}^..{end} --pretty=format:\"%H%x1f%h%x1f%an%x1f%ad%x1f%s%x1f%b%x1e\" --date=iso --encoding=UTF-8";
            string output = RunGitCommand(cmd);
            var lines = output.Split(new[] { '\x1e' }, StringSplitOptions.RemoveEmptyEntries);

            List<CommitEntry> commits = new();
            foreach (var line in lines)
            {
                var parts = line.Split('\x1f');
                if (parts.Length < 5) continue;
                string message = parts[4];
                string body = parts.Length > 5 ? parts[5] : "";
                // 过滤关键词
                if (FilterKeywords.Any(keyword => message.Contains(keyword)))
                    continue;

                message = message.Replace("\"", "'");
                DateTime dt = DateTime.Parse(parts[3]);
                commits.Add(new CommitEntry
                {
                    FullHash = parts[0],
                    ShortHash = parts[1],
                    Author = parts[2],
                    Date = dt.ToString("yyyy-MM-dd"),
                    Time = dt.ToString("HH:mm"),
                    Message = message,
                    IsCherryPick = !string.IsNullOrEmpty(body) &&
                                   body.IndexOf("cherry picked from", StringComparison.OrdinalIgnoreCase) >= 0
                });
            }

            return commits;
        }

        /// <summary>
        /// 通用Git拉取command
        /// </summary>
        /// <param name="arguments">传入对应命令</param>
        /// <returns></returns>
        private string RunGitCommand(string arguments)
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "git",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Application.dataPath,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using Process process = new() { StartInfo = psi };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
            catch (Exception e)
            {
                Debug.LogError("Git命令执行失败: " + e.Message);
                return "";
            }
        }

        private class CommitEntry
        {
            public string FullHash;
            public string ShortHash;
            public string Author;
            public string Date;
            public string Time;
            public string Message;
            public bool IsCherryPick;

            public override string ToString()
            {
                return $"{Time} | {Author} | {Message} | {ShortHash}";
            }
        }

        [Button("Git更新描述区间推送", ButtonSizes.Large)]
        public void CollectGitDesc()
        {
            if (string.IsNullOrEmpty(startCommit) || string.IsNullOrEmpty(endCommit))
            {
                Debug.LogError("请先选择起止 Commit");
                return;
            }

            // TODO 测试机器人，后续根据热更分支 - 记录，推送正式冒烟群
            var commits = GetCommitsBetween(startCommit, endCommit);
            string message = FormatGitCommitsGroupedByDate(commits);
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogError("无内容更新");
                return;
            }

            PushWechatRobot(message);
            Debug.Log(message);
            string branchName = RunGitCommand("rev-parse --abbrev-ref HEAD").Trim();
            ConfigImporterSettings.Inst.RefreshLastCommit(branchName, endCommit);
            RefreshCommitDesc();
        }

        private List<ValueDropdownItem<string>> GetStartCommitList()
        {
            // 拉取最近两天的 commit
            var output =
                RunGitCommand(
                    $"log --since=\"7 days ago\" --pretty=format:\"%cd|%H|%an|%s\" --date=format:\"%Y-%m-%d %H:%M:%S\" --encoding=UTF-8");
            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            List<ValueDropdownItem<string>> items = new();
            HashSet<string> seenHashes = new();
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length < 4) continue;
                string hash = parts[1];
                if (seenHashes.Contains(hash)) continue;
                seenHashes.Add(hash);

                string gitDesc = parts[3].Replace(GitIgnoreStr, "[Git地址...]").Replace("/", "|");
                string label = $"{parts[0]} - {parts[2]} - {gitDesc} - {hash.Substring(0, 8)}";
                items.Add(new ValueDropdownItem<string>(label, hash));
            }

            return items;
        }

        private List<ValueDropdownItem<string>> GetEndCommitList()
        {
            int fetchCount = 20;
            var output =
                RunGitCommand(
                    $"log -n {fetchCount} --pretty=format:\"%cd|%H|%an|%s\" --date=format:\"%Y-%m-%d %H:%M:%S\" --encoding=UTF-8");
            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            List<ValueDropdownItem<string>> items = new();
            HashSet<string> seenHashes = new();
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length < 4) continue;
                string hash = parts[1];
                if (seenHashes.Contains(hash)) continue;
                seenHashes.Add(hash);

                string gitDesc = parts[3].Replace(GitIgnoreStr, "[Git地址...]").Replace("/", "|");
                string label = $"{parts[0]} - {parts[2]} - {gitDesc} - {hash.Substring(0, 8)}";
                items.Add(new ValueDropdownItem<string>(label, hash));
            }

            return items;
        }

        private string FormatGitCommitsGroupedByDate(List<CommitEntry> commits)
        {
            if (commits == null || commits.Count == 0)
                return "";
            StringBuilder sb = new();
            sb.AppendLine($"Git");
            var grouped = commits
                .OrderBy(c => c.Date)
                .ThenBy(c => c.Time)
                .GroupBy(c => c.Date);
            foreach (var group in grouped)
            {
                sb.AppendLine($"【{group.Key}】");
                foreach (var c in group)
                {
                    string cherryPickStr = c.IsCherryPick ? " [遴选]" : "";
                    sb.AppendLine($"{c.Time} | {c.Author} | {c.Message}{cherryPickStr}");
                }
            }

            return sb.ToString().TrimEnd(); // 去掉尾部空行
        }

        private void RefreshCommitDesc()
        {
            string branchName = RunGitCommand("rev-parse --abbrev-ref HEAD").Trim();
            string lastcommitHash = ConfigImporterSettings.Inst.GetLastCommitByBranchName(branchName);
            if (!string.IsNullOrEmpty(lastcommitHash))
            {
                startCommit = lastcommitHash; // 只赋 hash
            }
        }

        public async void PushWechatRobot(string message)
        {
            const string testRobotHook =
                "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=d4b8cb9b-fc62-42c3-b6aa-53a8db00e687";
            using (HttpClient client = new())
            {
                StringContent content = new($"{{ \"msgtype\":\"text\",\"text\":{{\"content\":\"{message}\"}}}}",
                    Encoding.UTF8,
                    "application/json");
                HttpResponseMessage response = await client.PostAsync(testRobotHook, content);
                string result = await response.Content.ReadAsStringAsync();
                var ResponseData = Newtonsoft.Json.JsonConvert.DeserializeObject<WeChatRobotResponse>(result);
                if (response.IsSuccessStatusCode && ResponseData.errcode == 0)
                {
                    Debug.Log($"消息推送成功!");
                }
                else
                {
                    Debug.Log("发送失败");
                }
            }
        }

        #endregion
    }
}

#endif