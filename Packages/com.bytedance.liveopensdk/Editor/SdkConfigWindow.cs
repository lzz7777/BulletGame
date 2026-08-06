using System;
using UnityEngine;
using UnityEditor;

namespace Douyin.LiveOpenSDK.Editor
{
    public class SdkConfigWindow : EditorWindow
    {
        public const string Menu0 = "ByteGame/";
        public const string Menu1 = "ByteGame/LiveOpenSDK/";
        public const string Menu2 = "ByteGame/CloudSync/";
        public const string Menu3 = "Tools/";
        public const string Menu4 = "Tools/LiveOpenSDK/";
        public const string Menu5 = "Tools/CloudSync/";
        public const string ToolTitle = "抖音直播玩法配置";
        public const string Tag = ToolTitle;

        public static string UsageNoConfigWarning => $"若使用云同步能力，必须创建配置文件!  请访问Editor菜单：{Menu4}/{ToolTitle}";

        private static bool IsUsingCloudSync => CloudSyncUsageEditor?.IsUsingCloudSync() ?? false;

        public static ICloudSyncUsageEditor CloudSyncUsageEditor { get; set; }

        public interface ICloudSyncUsageEditor
        {
            bool IsUsingCloudSync();
            void SetUsingCloudSync(bool value);
        }


        #region Style Data Fields

        private Vector2 WindowMinSize { get; } = new(640, 400);

        private GUILayoutOption[] BigButtonOption { get; } = { GUILayout.Width(200), GUILayout.Height(50), GUILayout.ExpandWidth(false) };

        private GUILayoutOption[] ConfigItemLabelOption { get; } = { GUILayout.Width(_configItemLabelIndent), GUILayout.ExpandWidth(false) };

        private static readonly int _configItemLabelIndent = 250;
        private readonly int _configContentIndent = 10;

        private static GUIStyle _staticStyle;
        private GUIStyle _normalStyle;
        private GUIStyle _normalHeaderStyle;
        private GUIStyle _contentTextBoxStyle;
        private GUIStyle _singleLineBoxStyle;

        #endregion Style Data Fields


        private bool HasFile { get; set; }
        private bool HasConfig => Config != null;

        private DanmuConfig Config
        {
            get => _danmuConfigData;
            set
            {
                _danmuConfigData = value;
                OnConfigContentUpdate();
            }
        }

        private string LoadedConfigJson { get; set; }
        private DanmuFileUtils.FileTask<DanmuConfig> LoadedTask { get; set; }
        private static DanmuFileUtils DanmuFileUtils => DanmuFileUtils.Instance;

        // ReSharper disable once InconsistentNaming
        private DanmuConfig _danmuConfigData { get; set; }

        // ReSharper disable once InconsistentNaming
        private string _configJsonContent { get; set; }
        private bool IsModified => _isModified || LoadedConfigJson != _configJsonContent;

        private bool _isModified;

        private void OnConfigContentUpdate()
        {
            _configJsonContent = _danmuConfigData?.ToStr();
        }

        private void SetConfigContentModified(bool value = true)
        {
            _isModified = value;
            OnConfigContentUpdate();
            UpdateTitle();
        }

        private static SdkEditorGUI.GUIIcons Icons => SdkEditorGUI.Icons;

        private static readonly LiveOpenSDK.Utilities.SdkDebugLogger Debug = new(Tag)
        {
            IsTimeEnabled = false,
        };

        // note: 此菜单对SDK使用者、开发者可见
        [MenuItem(Menu0 + ToolTitle)]
        [MenuItem(Menu1 + ToolTitle)]
        [MenuItem(Menu2 + ToolTitle)]
        [MenuItem(Menu3 + ToolTitle)]
        [MenuItem(Menu4 + ToolTitle)]
        [MenuItem(Menu5 + ToolTitle)]
        public static void ShowWindow()
        {
            var hasOpen = HasOpenInstances<SdkConfigWindow>();
            if (hasOpen)
            {
                FocusWindowIfItsOpen<SdkConfigWindow>();
                return;
            }

            var window = GetWindow<SdkConfigWindow>();
            window.InitSize();
            window.InitPos();
            window.Show();
        }

        public static bool IsConfigValid()
        {
            return DanmuFileUtils.IsConfigExist() && DanmuFileUtils.LoadConfig().IsSuccess;
        }

        private void OnEnable()
        {
            UpdateTitle();
            LoadConfig();
        }

        private void OnDisable()
        {
        }


        #region Style

        private void InitSize()
        {
            minSize = WindowMinSize;
        }

        private void InitPos()
        {
            var rect = position;
            if (rect.position != Vector2.zero && rect.xMin > 0 && rect.yMin > 0)
                return;
            rect.width = Mathf.Max(rect.width, minSize.x);
            rect.height = Mathf.Max(rect.height, minSize.y);
            var parentRect = EditorGUIUtility.GetMainWindowPosition();
            rect.center = parentRect.center;
            position = rect;
        }

        private void UpdateTitle()
        {
            var text = ToolTitle + GetModifiedSymbol();
            titleContent = new GUIContent(text);
        }

        private string GetModifiedSymbol()
        {
            return IsModified ? " *" : "";
        }


        private void CheckInitStyle()
        {
            var hasInit = _staticStyle != null && _normalStyle != null;
            if (hasInit)
                return;

            _staticStyle = new GUIStyle();
            var label = GUI.skin.label;
            var box = GUI.skin.box;
            _normalStyle = new GUIStyle(label)
            {
                wordWrap = true
            };
            _normalHeaderStyle = new GUIStyle(label)
            {
                fontSize = label.fontSize + 2,
                fontStyle = FontStyle.Bold
            };
            _contentTextBoxStyle = new GUIStyle(box)
            {
                font = LoadFont("SpaceMono/SpaceMono-Regular.ttf"),
                alignment = TextAnchor.UpperLeft,
                fontSize = label.fontSize - 1,
                fontStyle = FontStyle.Normal,
                wordWrap = true,
            };
            _singleLineBoxStyle = new GUIStyle(box)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = false,
            };
        }

        private static Font LoadFont(string fontFile)
        {
            try
            {
                return AssetDatabase.LoadAssetAtPath<Font>($"Packages/com.bytedance.liveopensdk/Editor/Fonts/{fontFile}");
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion Style


        #region GUI

        private void OnGUI()
        {
            CheckInitStyle();
            Icons.CheckInitRes();

            GUILayout.Space(10);

            if (!HasConfig)
                GUI_WarnNeedCreate();

            GUI_EditFields();
            GUI_MainButtons();

            GUI_ContentTextBox();
            GUI_OtherButtons();
        }

        private void GUI_WarnNeedCreate()
        {
            using (new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Space(10);
                    EditorGUILayout.HelpBox("提示:\n检测到使用LiveOpenSDK，请创建抖音直播玩法配置文件!", MessageType.Warning, true);

                    if (IsUsingCloudSync)
                    {
                        GUILayout.Space(10);
                        using (new GUILayout.HorizontalScope())
                        {
                            EditorGUILayout.HelpBox("提示:\n若使用云同步能力，必须创建配置文件、并开启云同步!", MessageType.Warning, true);
                            GUI_IgnoreIsUsingCloudSync();
                        }
                    }

                    GUILayout.Space(10);
                    if (HasFile && Config == null)
                        EditorGUILayout.HelpBox("错误:\n配置文件损坏，必须重新创建!", MessageType.Error, true);
                }
            }
        }

        private void GUI_IgnoreIsUsingCloudSync()
        {
            if (IsUsingCloudSync && CloudSyncUsageEditor != null && (Config == null || Config.cloudsync == false))
            {
                GUILayout.Space(10);
                if (GUILayout.Button("忽略：\n（未使用云同步能力）", GUILayout.ExpandWidth(false)))
                {
                    CloudSyncUsageEditor.SetUsingCloudSync(false);
                }

                GUILayout.Space(10);
            }
        }

        private void GUI_EditFields()
        {
            if (!HasConfig)
                return;

            GUILayout.Space(10);
            GUILayout.Label("修改配置：", _normalHeaderStyle);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(_configContentIndent);

                using (new GUILayout.VerticalScope())
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("开启云同步", ConfigItemLabelOption);
                        var prev = Config.cloudsync;
                        var value = GUILayout.Toggle(prev, "");
                        if (value != prev)
                        {
                            Config.cloudsync = value;
                            SetConfigContentModified();
                        }

                        GUILayout.Space(_configContentIndent);
                    }

                    if (HasConfig && IsUsingCloudSync && !Config.cloudsync)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            EditorGUILayout.HelpBox("提示: 检测到你可能在使用云同步能力，若确认使用，请务必开启云同步", MessageType.Warning);
                            GUI_IgnoreIsUsingCloudSync();
                        }
                    }

                    using (new EditorGUI.DisabledGroupScope(!Config.cloudsync))
                    {
                        var prev = DanmuConfig.NullableBoolToInt(Config.mate_live_local);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("允许伴侣本地开播", ConfigItemLabelOption);
                            var tabModeNames = new[] { "默认(由平台决定)", "是", "否" };
                            var value = GUILayout.SelectionGrid(prev, tabModeNames, 3);
                            if (value != prev)
                            {
                                Config.mate_live_local = DanmuConfig.IntToNullableBool(value);
                                SetConfigContentModified();
                            }

                            GUILayout.Space(_configContentIndent);
                        }
                    }
                }
            }
        }

        private void GUI_ContentTextBox()
        {
            if (!HasConfig)
                return;

            GUILayout.Space(20);
            var filename = DanmuFileUtils.ConfigFileName;
            GUILayout.Label($"配置内容: {(IsModified ? " (*待保存)" : "")} {filename}");
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(_configContentIndent);
                GUILayout.Box(_configJsonContent, _contentTextBoxStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(_configContentIndent);
            }
        }

        private void GUI_MainButtons()
        {
            GUILayout.Space(10);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (!HasConfig)
                {
                    if (GUILayout.Button(new GUIContent("创建配置文件", Icons.SaveActive), BigButtonOption))
                    {
                        SaveConfig(new DanmuConfig
                        {
                            cloudsync = IsUsingCloudSync
                        });
                    }
                }

                GUILayout.Space(20);

                if (GUILayout.Button(new GUIContent("重新读取", Icons.Refresh), BigButtonOption))
                {
                    LoadConfig();
                }

                if (!HasConfig)
                {
                    GUILayout.FlexibleSpace();
                    return;
                }

                GUILayout.Space(20);
                var modSymbol = GetModifiedSymbol();
                if (GUILayout.Button(new GUIContent("保存配置" + modSymbol, Icons.SaveActive), BigButtonOption))
                {
                    Debug.Assert(Config != null, "Assert DanmuConfig != null");
                    SaveConfig(Config);
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void GUI_OtherButtons()
        {
            GUILayout.Space(10);

            if (HasConfig)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(_configContentIndent);
                    if (GUILayout.Button("配置文件位置", GUILayout.ExpandWidth(false)))
                        ShowConfigLocation();

                    GUILayout.Box($"{DanmuFileUtils.ConfigFilePath}", _singleLineBoxStyle);
                }
            }
        }

        #endregion


        private void ShowConfigLocation()
        {
            var path = DanmuFileUtils.ConfigFilePath;
            EditorUtility.RevealInFinder(path);
        }

        private void LoadConfig()
        {
            Config = null;
            HasFile = DanmuFileUtils.IsConfigExist();
            var task = DanmuFileUtils.LoadConfig();
            LoadedTask = task;
            if (task.IsSuccess && task.Data != null)
            {
                Config = task.Data;
                LoadedConfigJson = task.JsonString;
                SetConfigContentModified(false);
                Debug.Log($"读取配置成功: \n{task.Data.ToStr()}");
            }
            else
            {
                Config = null;
                LoadedConfigJson = null;
                var msg = $"读取配置失败: {task.Path} {task.ErrorMsg}";
                if (HasFile)
                    Debug.LogError(msg);
                else
                    Debug.LogWarning(msg);
            }
        }

        private void SaveConfig(DanmuConfig config)
        {
            Debug.Assert(config != null, "Assert config != null");
            var task = DanmuFileUtils.SaveConfig(config);
            if (task.IsSuccess && task.Data != null)
            {
                Config = task.Data;
                LoadedConfigJson = task.JsonString;
                SetConfigContentModified(false);
                var msg = "保存配置成功";
                Debug.Log(msg + $"\n{task.Data.ToStr()}");
                EditorUtility.DisplayDialog(ToolTitle, msg, "OK");
            }
            else
            {
                var msg = $"保存配置失败: {task.ErrorMsg}";
                Debug.LogError(msg);
                EditorUtility.DisplayDialog(ToolTitle + " - 错误", msg, "OK");
            }
        }
    }
}