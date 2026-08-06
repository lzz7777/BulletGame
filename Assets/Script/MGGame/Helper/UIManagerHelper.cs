using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
    public static class UIManagerHelper
    {
        public static void Close<T>(this T self) where T : UIPanelBase => self.CloseAsync<T>().ToCoroutine();

        public static async UniTask CloseAsync<T>(this T self) where T : UIPanelBase =>
            UIManager.Instance.CloseWindow<T>().ToCoroutine();

        public static void OpenWindow<T>(this UIPanelBase self, UIWindowData uIWindowData = null) where T : UIPanelBase =>
            self.OpenWindowAsync<T>(uIWindowData).ToCoroutine();

        public static async UniTask OpenWindowAsync<T>(this UIPanelBase self, UIWindowData uIWindowData = null) where T : UIPanelBase =>
            await UIManager.Instance.OpenWindow<T>();

        public static void OpenSubWindow<T>(this UIPanelBase self, RectTransform parentNode,
            UIWindowData uIWindowData = null) where T : UISubViewBase =>
            self.OpenSubWindowAsync<T>(parentNode, uIWindowData).ToCoroutine();

        public static async UniTask OpenSubWindowAsync<T>(this UIPanelBase uIPanelBase, RectTransform parentNode,
            UIWindowData uIWindowData) where T : UISubViewBase
        {
            var typeName = typeof(T).Name;

            if (parentNode == null)
                return;

            if (!uIPanelBase.SubViews.TryGetValue(typeName, out var subView))
            {
                var go = await ObjectPoolManager.Instance.GetFromPool(typeName, parentNode);
                subView = go.GetComponent<UISubViewBase>();
                uIPanelBase.SubViews[typeName] = subView;
            }

            if (subView.IsOpen)
                return;

            subView.gameObject.SetActive(true);
            subView.IsOpen = true;
            subView.OnOpen(uIWindowData);
        }

        public static void CloseSubWindow<T>(this UIPanelBase uIPanelBase) where T : UISubViewBase =>
            CloseSubWindowAsync(uIPanelBase, typeof(T).Name).ToCoroutine();

        public static async UniTask CloseSubWindowAsync(this UIPanelBase uIPanelBase, string typeName)
        {
            if (!uIPanelBase.SubViews.TryGetValue(typeName, out var subView))
            {
                return;
            }

            if (!subView.IsOpen)
                return;

            subView.gameObject.SetActive(false);
            subView.IsOpen = false;
            subView.OnClose();
        }

        /// <summary>
        /// 传入数值，显示UI数值（万上：两位小数+向上取整+万亿单位； 万下：向上取整）
        /// </summary>
        /// <param name="x"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public static string UIMathCeil(double x, int decimals = 2, bool forceDecimals = false)
        {
            if (decimals < 0) decimals = 0;

            decimal dx = (decimal)x;
            string unit = string.Empty;
            decimal divisor = 1m;

            // 单位换算：按绝对值判断大小
            decimal abs = Math.Abs(dx);
            if (abs >= 100_000_000m)
            {
                unit = "亿";
                divisor = 100_000_000m;
            }
            else if (abs >= 10_000m)
            {
                unit = "万";
                divisor = 10_000m;
            }
            else
            {
                decimals = 0;
            }

            decimal scaled = dx / divisor;
            decimal rounded = Math.Round(scaled, decimals, MidpointRounding.AwayFromZero);
            char formatChar = forceDecimals ? '0' : '#';
            string format = decimals <= 0 ? "0" : "0." + new string(formatChar, decimals);
            return rounded.ToString(format, CultureInfo.InvariantCulture) + unit;
        }

        private static decimal Pow10(int n)
        {
            decimal r = 1m;
            for (int i = 0; i < n; i++) r *= 10m;
            return r;
        }

        public static bool IsEqual(double a, double b)
        {
            double epsilon = 1e-10; // 容差值（根据精度需求调整）
            return Math.Abs(a - b) < epsilon;
        }

        /// <summary>
        /// 缩放显示隐藏
        /// </summary>
        /// <param name="comp"></param>
        /// <param name="active"></param>
        public static void SetActiveScale(this Component comp, bool active)
        {
            comp.transform.localScale = active ? Vector3.one : Vector3.zero;
        }

        /// <summary>
        /// 获取实际宽度
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <returns></returns>
        public static float GetActualWidth(this RectTransform rectTransform)
        {
            return rectTransform.rect.width * rectTransform.localScale.x;
        }
    }
}