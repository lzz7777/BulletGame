using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [AddComponentMenu("Event/Graphic Raycaster")]
    [RequireComponent(typeof(Canvas))]
    /// <summary>
    /// BaseRaycaster 的派生类，专门用于针对 Graphic（视觉）元素进行射线检测。
    /// </summary>
    // [重点注释 - 射线检测控制器]
    // 每个包含交互元素的 Canvas 都会挂载此组件，它是连接 EventSystem（输入）和具体 UI 组件（响应）的桥梁。
    // 在每帧由 EventSystem 发起的射线检测中，它会收集此 Canvas 下所有启用了 RaycastTarget 的组件，并判断其是否被指针命中。
    public class GraphicRaycaster : BaseRaycaster
    {
        protected const int kNoEventMaskSet = -1;

        /// <summary>
        /// 用于检查阻挡 Canvas 元素的射线检测类型。
        /// </summary>
        public enum BlockingObjects
        {
            /// <summary>
            /// 不执行任何物理射线检测。
            /// </summary>
            None = 0,
            /// <summary>
            /// 执行 2D 物理射线检测，以检查是否有 2D 元素阻挡。
            /// </summary>
            TwoD = 1,
            /// <summary>
            /// 执行 3D 物理射线检测，以检查是否有 3D 元素阻挡。
            /// </summary>
            ThreeD = 2,
            /// <summary>
            /// 执行 2D 和 3D 物理射线检测，以检查是否有 2D 和 3D 元素阻挡。
            /// </summary>
            All = 3,
        }

        /// <summary>
        /// 基于排序顺序 (sort order) 的射线检测器优先级。
        /// </summary>
        /// <returns>
        /// sortOrder 优先级。
        /// </returns>
        public override int sortOrderPriority
        {
            get
            {
                // We need to return the sorting order here as distance will all be 0 for overlay.
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return canvas.sortingOrder;

                return base.sortOrderPriority;
            }
        }

        /// <summary>
        /// 基于渲染顺序 (render order) 的射线检测器优先级。
        /// </summary>
        /// <returns>
        /// renderOrder 优先级。
        /// </returns>
        public override int renderOrderPriority
        {
            get
            {
                // We need to return the sorting order here as distance will all be 0 for overlay.
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return canvas.rootCanvas.renderOrder;

                return base.renderOrderPriority;
            }
        }

        [FormerlySerializedAs("ignoreReversedGraphics")]
        [SerializeField]
        private bool m_IgnoreReversedGraphics = true;
        [FormerlySerializedAs("blockingObjects")]
        [SerializeField]
        private BlockingObjects m_BlockingObjects = BlockingObjects.None;

        /// <summary>
        /// 是否忽略背对射线检测器的图形（即背面剔除）？
        /// </summary>
        public bool ignoreReversedGraphics { get {return m_IgnoreReversedGraphics; } set { m_IgnoreReversedGraphics = value; } }

        /// <summary>
        /// 用于确定哪些类型的对象可以阻挡图形射线检测 (Graphic Raycasts)。
        /// </summary>
        public BlockingObjects blockingObjects { get {return m_BlockingObjects; } set { m_BlockingObjects = value; } }

        [SerializeField]
        protected LayerMask m_BlockingMask = kNoEventMaskSet;

        /// <summary>
        /// 通过 LayerMask 指定的阻挡对象类型，用于确定它们是否阻挡图形射线检测。
        /// </summary>
        public LayerMask blockingMask { get { return m_BlockingMask; } set { m_BlockingMask = value; } }

        private Canvas m_Canvas;

        protected GraphicRaycaster()
        {}

        private Canvas canvas
        {
            get
            {
                if (m_Canvas != null)
                    return m_Canvas;

                m_Canvas = GetComponent<Canvas>();
                return m_Canvas;
            }
        }

        [NonSerialized] private List<Graphic> m_RaycastResults = new List<Graphic>();

        /// <summary>
        /// 针对与 Canvas 关联的图形列表执行射线检测。
        /// </summary>
        /// <param name="eventData">当前事件数据</param>
        /// <param name="resultAppendList">用于追加新命中结果的列表。</param>
        // [重点注释 - 射线检测主入口]
        // 这是 GraphicRaycaster 的核心逻辑。它不仅包含遍历 UI，还包含对物理阻挡的检测。
        // 如果 blockingObjects 开启，它会先通过 ReflectionMethodsCache 发射一次 3D 或 2D 的物理射线，
        // 找到物理碰撞距离（hitDistance），如果命中 UI 元素的距离大于这个物理距离，UI 命中就会被丢弃。
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (canvas == null)
                return;

            // [重点注释 - 第三主线：事件与射线检测系统]
            // GraphicRaycaster 是如何知道你点中了哪个 UI 的？
            // 核心思路：它不会对整个 UI 树做物理射线检测，而是从 GraphicRegistry 中获取
            // 当前 Canvas 下所有注册了 RaycastTarget 的 UI 元素（这是一个扁平的列表）。
            // 因此，将不需要交互的 UI 元素的 RaycastTarget 设为 false，可以有效减少这里的遍历数量，提升性能。
            var canvasGraphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
            if (canvasGraphics == null || canvasGraphics.Count == 0)
                return;

            int displayIndex;
            var currentEventCamera = eventCamera; // Property can call Camera.main, so cache the reference

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || currentEventCamera == null)
                displayIndex = canvas.targetDisplay;
            else
                displayIndex = currentEventCamera.targetDisplay;

            Vector3 eventPosition = MultipleDisplayUtilities.GetRelativeMousePositionForRaycast(eventData);

            // 丢弃不属于当前显示器 (Display) 的事件，这样用户就不会同时与多个显示器交互。
            if ((int) eventPosition.z != displayIndex)
                return;

            // 转换为视口空间 (View Space) 坐标
            Vector2 pos;
            if (currentEventCamera == null)
            {
                // 多显示器支持仅在非主显示器时生效。对于显示器 0，由于它是显示 API 的一部分，
                // 报告的分辨率始终是桌面的分辨率，因此我们使用标准非多显示器的方法。
                float w = Screen.width;
                float h = Screen.height;
                if (displayIndex > 0 && displayIndex < Display.displays.Length)
                {
                    w = Display.displays[displayIndex].systemWidth;
                    h = Display.displays[displayIndex].systemHeight;
                }
                pos = new Vector2(eventPosition.x / w, eventPosition.y / h);
            }
            else
                pos = currentEventCamera.ScreenToViewportPoint(eventPosition);

            // 如果点击位置在相机的视口范围之外，则不执行任何操作
            if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f)
                return;

            float hitDistance = float.MaxValue;

            Ray ray = new Ray();

            if (currentEventCamera != null)
                ray = currentEventCamera.ScreenPointToRay(eventPosition);

            // [重点注释 - 物理遮挡检测]
            // 如果不是 Overlay 模式且开启了物理阻挡检测 (blockingObjects != None)，
            // 此时发射 3D/2D 物理射线。利用反射缓存 (ReflectionMethodsCache) 调用物理引擎接口，
            // 找到物理碰撞点距离相机的最近距离 (hitDistance)。
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && blockingObjects != BlockingObjects.None)
            {
                float distanceToClipPlane = 100.0f;

                if (currentEventCamera != null)
                {
                    float projectionDirection = ray.direction.z;
                    distanceToClipPlane = Mathf.Approximately(0.0f, projectionDirection)
                        ? Mathf.Infinity
                        : Mathf.Abs((currentEventCamera.farClipPlane - currentEventCamera.nearClipPlane) / projectionDirection);
                }
#if PACKAGE_PHYSICS
                if (blockingObjects == BlockingObjects.ThreeD || blockingObjects == BlockingObjects.All)
                {
                    if (ReflectionMethodsCache.Singleton.raycast3D != null)
                    {
                        RaycastHit hit;
                        if (ReflectionMethodsCache.Singleton.raycast3D(ray, out hit, distanceToClipPlane, (int)m_BlockingMask))
                        {
                            hitDistance = hit.distance;
                        }
                    }
                }
#endif
#if PACKAGE_PHYSICS2D
                if (blockingObjects == BlockingObjects.TwoD || blockingObjects == BlockingObjects.All)
                {
                    if (ReflectionMethodsCache.Singleton.raycast2D != null)
                    {
                        var hits = ReflectionMethodsCache.Singleton.getRayIntersectionAll(ray, distanceToClipPlane, (int)m_BlockingMask);
                        if (hits.Length > 0)
                            hitDistance = hits[0].distance;
                    }
                }
#endif
            }

            m_RaycastResults.Clear();

            // 调用内部静态 Raycast 方法，获取所有被点击的、有效的 UI Graphic
            Raycast(canvas, currentEventCamera, eventPosition, canvasGraphics, m_RaycastResults);

            int totalCount = m_RaycastResults.Count;
            for (var index = 0; index < totalCount; index++)
            {
                var go = m_RaycastResults[index].gameObject;
                bool appendGraphic = true;

                // [重点注释 - 背面剔除检测]
                if (ignoreReversedGraphics)
                {
                    if (currentEventCamera == null)
                    {
                        // 如果没有相机，我们假定 UI 始终朝向前方 (Vector3.forward)
                        var dir = go.transform.rotation * Vector3.forward;
                        appendGraphic = Vector3.Dot(Vector3.forward, dir) > 0;
                    }
                    else
                    {
                        // 如果有相机，将 UI 朝向与相机前方进行点乘比较，以判断其是否背对相机。
                        var cameraForward = currentEventCamera.transform.rotation * Vector3.forward * currentEventCamera.nearClipPlane;
                        appendGraphic = Vector3.Dot(go.transform.position - currentEventCamera.transform.position - cameraForward, go.transform.forward) >= 0;
                    }
                }

                if (appendGraphic)
                {
                    float distance = 0;
                    Transform trans = go.transform;
                    Vector3 transForward = trans.forward;

                    if (currentEventCamera == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        distance = 0;
                    else
                    {
                        // 计算射线与平面的相交距离: http://geomalgorithms.com/a06-_intersect-2.html
                        distance = (Vector3.Dot(transForward, trans.position - ray.origin) / Vector3.Dot(transForward, ray.direction));

                        // 检查游戏对象是否在相机背后。如果是，则跳过。
                        if (distance < 0)
                            continue;
                    }

                    // [重点注释 - 物理遮挡最终判断]
                    // 如果 UI 到相机的距离 (distance) >= 物理碰撞距离 (hitDistance)
                    // 说明 UI 被前方的 3D/2D 物理模型挡住了，直接 continue 丢弃该命中结果。
                    if (distance >= hitDistance)
                        continue;

                    var castResult = new RaycastResult
                    {
                        gameObject = go,
                        module = this,
                        distance = distance,
                        screenPosition = eventPosition,
                        displayIndex = displayIndex,
                        index = resultAppendList.Count,
                        depth = m_RaycastResults[index].depth,
                        sortingLayer = canvas.sortingLayerID,
                        sortingOrder = canvas.sortingOrder,
                        worldPosition = ray.origin + ray.direction * distance,
                        worldNormal = -transForward
                    };
                    resultAppendList.Add(castResult);
                }
            }
        }

        /// <summary>
        /// 将为该射线检测器生成射线的相机。
        /// </summary>
        /// <returns>
        /// - 如果 Canvas 渲染模式为 ScreenSpaceOverlay 或 ScreenSpaceCamera 且没有指定相机，则返回 Null。
        /// - 如果不为空，则返回 canvas.worldCamera。
        /// - 返回 Camera.main。
        /// </returns>
        public override Camera eventCamera
        {
            get
            {
                var canvas = this.canvas;
                var renderMode = canvas.renderMode;
                if (renderMode == RenderMode.ScreenSpaceOverlay
                    || (renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null))
                    return null;

                return canvas.worldCamera ?? Camera.main;
            }
        }

        /// <summary>
        /// 向屏幕发射一条射线并收集其下方的所有图形 (Graphic)。
        /// </summary>
        [NonSerialized] static readonly List<Graphic> s_SortedGraphics = new List<Graphic>();
        private static void Raycast(Canvas canvas, Camera eventCamera, Vector2 pointerPosition, IList<Graphic> foundGraphics, List<Graphic> results)
        {
            // Necessary for the event system
            int totalCount = foundGraphics.Count;
            for (int i = 0; i < totalCount; ++i)
            {
                Graphic graphic = foundGraphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                if (!graphic.raycastTarget || graphic.canvasRenderer.cull || graphic.depth == -1)
                    continue;

                // [重点注释 - 射线相交测试]
                // 遍历符合条件的 Graphic，先利用 RectTransform 的包围盒做快速的屏幕坐标相交测试。
                // 如果在包围盒内，后续才会继续调用 graphic.Raycast 做精确判断（例如 Image 上的 alphaHitTestMinimumThreshold 检测）。
                if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, pointerPosition, eventCamera, graphic.raycastPadding))
                    continue;

                if (eventCamera != null && eventCamera.WorldToScreenPoint(graphic.rectTransform.position).z > eventCamera.farClipPlane)
                    continue;

                if (graphic.Raycast(pointerPosition, eventCamera))
                {
                    s_SortedGraphics.Add(graphic);
                }
            }

            s_SortedGraphics.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));
            totalCount = s_SortedGraphics.Count;
            for (int i = 0; i < totalCount; ++i)
                results.Add(s_SortedGraphics[i]);

            s_SortedGraphics.Clear();
        }
    }
}
