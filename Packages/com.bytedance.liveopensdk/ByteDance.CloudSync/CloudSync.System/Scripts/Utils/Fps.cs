using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 用于实时显示塔防当前游戏帧数，仅供调试测试使用
    /// </summary>
    public class Fps : MonoBehaviour, IPointerDownHandler
    {
        public Text fpsText;
        public Text pointerDownText;

        private float deltaTime = 0.0f;

        void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f; // 平均化处理
            float fps = 1.0f / deltaTime;
            fpsText.text = $"{Mathf.RoundToInt(fps)} FPS #{Time.frameCount}";
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerDownText != null) {
                pointerDownText.text = $"Last Down:\n#{Time.frameCount}";
            }
        }
    }
}
