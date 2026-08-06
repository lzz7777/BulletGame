// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/07/26
// Description:

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ByteDance.CloudSync.Editor
{
    /// <summary>
    /// Editor面板调试 Cloud Screen 1~4
    /// </summary>
    internal class VisualScreenWindow : EditorWindow
    {
        // note: 此菜单对SDK使用者、开发者可见
        [MenuItem("ByteGame/CloudSync/Screen1")]
        private static void Open1() => Open(SeatIndex.Index0);

        [MenuItem("ByteGame/CloudSync/Screen2")]
        private static void Open2() => Open(SeatIndex.Index1);

        [MenuItem("ByteGame/CloudSync/Screen3")]
        private static void Open3() => Open(SeatIndex.Index2);

        [MenuItem("ByteGame/CloudSync/Screen4")]
        private static void Open4() => Open(SeatIndex.Index3);

        private static void Open(SeatIndex index)
        {
            if (Windows.ContainsKey(index))
                return;
            var w = CreateWindow<VisualScreenWindow>($"Screen - {index}");
            w.SetIndex(index);
        }

        [SerializeField]
        private SeatIndex index = SeatIndex.Invalid;

        private VisualScreenView _view;
        private static readonly Dictionary<SeatIndex, VisualScreenWindow> Windows = new();

        private void SetIndex(SeatIndex value)
        {
            index = value;
            if (Windows.TryGetValue(value, out var old))
                old.Close();
            Windows[value] = this;
        }

        private void Awake()
        {
            if (index < 0)
                return;
            SetIndex(index);
        }

        private void OnGUI()
        {
            _view ??= new VisualScreenView(this, index);
            _view.Draw(new Rect(0, 0, position.width, position.height));
            if (Application.isPlaying)
                Repaint();
        }

        private void OnDestroy()
        {
            Windows.Remove(index);
            _view?.Dispose();
        }
    }
}