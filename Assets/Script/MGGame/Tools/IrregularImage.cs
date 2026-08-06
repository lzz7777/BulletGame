using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    /// <summary>
    /// 基于多边形命中测试图片点击事件
    /// </summary>
    [RequireComponent(typeof(PolygonCollider2D))]
    public class IrregularImage : Image
    {
        private PolygonCollider2D _polygonCollider;

        protected override void Awake()
        {
            base.Awake();
            _polygonCollider = GetComponent<PolygonCollider2D>();
        }

        // 重写精筛逻辑
        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (_polygonCollider == null)
                _polygonCollider = GetComponent<PolygonCollider2D>();

            if (_polygonCollider == null)
                return false;

            // 【核心修复】：使用 ScreenPointToWorldPointInRectangle 获取世界坐标
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, screenPoint, eventCamera,
                    out Vector3 worldPoint))
            {
                // 将世界坐标传给物理引擎进行多边形重叠测试
                bool isHit = _polygonCollider.OverlapPoint(worldPoint);

                // 排查辅助：你可以临时取消注释下面这行，如果在按钮上晃动鼠标狂刷 true，说明底层检测通了
                // Debug.Log($"[IrregularImage] 坐标:{worldPoint} | 命中:{isHit}");

                return isHit;
            }

            return false;
        }
    }
}