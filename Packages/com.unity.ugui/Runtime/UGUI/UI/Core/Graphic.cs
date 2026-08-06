using System;
#if UNITY_EDITOR
using System.Reflection;
#endif
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI.CoroutineTween;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    /// <summary>
    /// 所有视觉 UI 组件的基类。当创建自定义的 UI 视觉类型时，应继承此类。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    // [重点注释 - UGUI 渲染基类]
    // 这是所有可见 UI 元素（如 Image、Text）的基类。
    // 它负责管理 UI 的基础属性（颜色、材质、射线检测等），并实现了 ICanvasElement 接口，
    // 以便将自己注册到 CanvasUpdateRegistry 的重建管线中。
    // 核心设计模式是：在各种属性（颜色/位置）的 Setter 中调用 SetXxxDirty 标记自身，
    // 然后在渲染前统一调用 Rebuild -> UpdateGeometry/UpdateMaterial 来真正生成网格和更新材质。
    /// <summary>
    ///   所有视觉 UI 组件的基类。
    ///   当创建视觉 UI 组件时，你应该继承这个类。
    /// </summary>
    /// <example>
    /// Below is a simple example that draws a colored quad inside the Rect Transform area.
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.UI;
    ///
    /// [ExecuteInEditMode]
    /// public class SimpleImage : Graphic
    /// {
    ///     protected override void OnPopulateMesh(VertexHelper vh)
    ///     {
    ///         Vector2 corner1 = Vector2.zero;
    ///         Vector2 corner2 = Vector2.zero;
    ///
    ///         corner1.x = 0f;
    ///         corner1.y = 0f;
    ///         corner2.x = 1f;
    ///         corner2.y = 1f;
    ///
    ///         corner1.x -= rectTransform.pivot.x;
    ///         corner1.y -= rectTransform.pivot.y;
    ///         corner2.x -= rectTransform.pivot.x;
    ///         corner2.y -= rectTransform.pivot.y;
    ///
    ///         corner1.x *= rectTransform.rect.width;
    ///         corner1.y *= rectTransform.rect.height;
    ///         corner2.x *= rectTransform.rect.width;
    ///         corner2.y *= rectTransform.rect.height;
    ///
    ///         vh.Clear();
    ///
    ///         UIVertex vert = UIVertex.simpleVert;
    ///
    ///         vert.position = new Vector2(corner1.x, corner1.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vert.position = new Vector2(corner1.x, corner2.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vert.position = new Vector2(corner2.x, corner2.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vert.position = new Vector2(corner2.x, corner1.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vh.AddTriangle(0, 1, 2);
    ///         vh.AddTriangle(2, 3, 0);
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public abstract class Graphic
        : UIBehaviour,
          ICanvasElement
    {
        static protected Material s_DefaultUI = null;
        static protected Texture2D s_WhiteTexture = null;

        /// <summary>
        /// 如果未明确指定材质，则用于绘制 UI 元素的默认材质。
        /// </summary>

        static public Material defaultGraphicMaterial
        {
            get
            {
                if (s_DefaultUI == null)
                    s_DefaultUI = Canvas.GetDefaultCanvasMaterial();
                return s_DefaultUI;
            }
        }

        // Cached and saved values
        [FormerlySerializedAs("m_Mat")]
        [SerializeField] protected Material m_Material;

        [SerializeField] private Color m_Color = Color.white;

        [NonSerialized] protected bool m_SkipLayoutUpdate;
        [NonSerialized] protected bool m_SkipMaterialUpdate;
         
        /// <summary>
        /// Graphic 的基础颜色。
        /// </summary>
        /// <remarks>
        /// 内置的 UI 组件使用此颜色作为其顶点颜色。你可以使用此属性来获取或更改视觉 UI 元素（例如 Image）的颜色。
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// //Place this script on a GameObject with a Graphic component attached e.g. a visual UI element (Image).
        ///
        /// using UnityEngine;
        /// using UnityEngine.UI;
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     Graphic m_Graphic;
        ///     Color m_MyColor;
        ///
        ///     void Start()
        ///     {
        ///         //Fetch the Graphic from the GameObject
        ///         m_Graphic = GetComponent<Graphic>();
        ///         //Create a new Color that starts as red
        ///         m_MyColor = Color.red;
        ///         //Change the Graphic Color to the new Color
        ///         m_Graphic.color = m_MyColor;
        ///     }
        ///
        ///     // Update is called once per frame
        ///     void Update()
        ///     {
        ///         //When the mouse button is clicked, change the Graphic Color
        ///         if (Input.GetKey(KeyCode.Mouse0))
        ///         {
        ///             //Change the Color over time between blue and red while the mouse button is pressed
        ///             m_MyColor = Color.Lerp(Color.red, Color.blue, Mathf.PingPong(Time.time, 1));
        ///         }
        ///         //Change the Graphic Color to the new Color
        ///         m_Graphic.color = m_MyColor;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual Color color { get { return m_Color; } set { if (SetPropertyUtility.SetColor(ref m_Color, value)) SetVerticesDirty(); } }

        [SerializeField] private bool m_RaycastTarget = true;

        private bool m_RaycastTargetCache = true;

        /// <summary>
        /// 此图形是否应被视为射线检测的目标？
        /// </summary>
        public virtual bool raycastTarget
        {
            get
            {
                return m_RaycastTarget;
            }
            set
            {
                if (value != m_RaycastTarget)
                {
                    if (m_RaycastTarget)
                        GraphicRegistry.UnregisterRaycastGraphicForCanvas(canvas, this);

                    m_RaycastTarget = value;

                    if (m_RaycastTarget && isActiveAndEnabled)
                        GraphicRegistry.RegisterRaycastGraphicForCanvas(canvas, this);
                }
                m_RaycastTargetCache = value;
            }
        }

        [SerializeField]
        private Vector4 m_RaycastPadding = new Vector4();

        /// <summary>
        /// 应用于射线遮罩检测的边距。
        /// X = Left (左)
        /// Y = Bottom (下)
        /// Z = Right (右)
        /// W = Top (上)
        /// </summary>
        public Vector4 raycastPadding
        {
            get { return m_RaycastPadding; }
            set
            {
                m_RaycastPadding = value;
            }
        }

        [NonSerialized] private RectTransform m_RectTransform;
        [NonSerialized] private CanvasRenderer m_CanvasRenderer;
        [NonSerialized] private Canvas m_Canvas;

        [NonSerialized] private bool m_VertsDirty;
        [NonSerialized] private bool m_MaterialDirty;

        [NonSerialized] protected UnityAction m_OnDirtyLayoutCallback;
        [NonSerialized] protected UnityAction m_OnDirtyVertsCallback;
        [NonSerialized] protected UnityAction m_OnDirtyMaterialCallback;

        [NonSerialized] protected static Mesh s_Mesh;
        [NonSerialized] private static readonly VertexHelper s_VertexHelper = new VertexHelper();

        [NonSerialized] protected Mesh m_CachedMesh;
        [NonSerialized] protected Vector2[] m_CachedUvs;
        // Tween controls for the Graphic
        [NonSerialized]
        private readonly TweenRunner<ColorTween> m_ColorTweenRunner;

        protected bool useLegacyMeshGeneration { get; set; }

        // Called by Unity prior to deserialization,
        // should not be called by users
        protected Graphic()
        {
            if (m_ColorTweenRunner == null)
                m_ColorTweenRunner = new TweenRunner<ColorTween>();
            m_ColorTweenRunner.Init(this);
            useLegacyMeshGeneration = true;
        }

        /// <summary>
        /// 将 Graphic 的所有属性标记为“脏”（dirty），表明它们需要被重建。
        /// 脏标记包括：布局 (Layout)、顶点 (Vertices) 和 材质 (Materials)。
        /// </summary>
        // [重点注释 - 全量脏标记]
        // 常用在组件被激活 (OnEnable) 或 RectTransform 维度发生剧烈变化时。
        // 它会一次性将自身注册到 Layout 和 Graphic 两个重建队列中。
        public virtual void SetAllDirty()
        {
            // Optimization: Graphic layout doesn't need recalculation if
            // the underlying Sprite is the same size with the same texture.
            // (e.g. Sprite sheet texture animation)

            if (m_SkipLayoutUpdate)
            {
                m_SkipLayoutUpdate = false;
            }
            else
            {
                SetLayoutDirty();
            }

            if (m_SkipMaterialUpdate)
            {
                m_SkipMaterialUpdate = false;
            }
            else
            {
                SetMaterialDirty();
            }

            SetVerticesDirty();
            SetRaycastDirty();
        }

        /// <summary>
        /// 将布局 (layout) 标记为脏 (dirty)，表明其需要被重建。
        /// </summary>
        /// <remarks>
        /// 如果有任何元素注册了此回调，则发送 OnDirtyLayoutCallback 通知。请参见 RegisterDirtyLayoutCallback。
        /// </remarks>
        public virtual void SetLayoutDirty()
        {
            if (!IsActive())
                return;

            // [重点注释 - 脏标记模式 (Dirty Pattern)]
            // 当 UI 大小或锚点等发生变化时，将自身及其父级 Layout 组件标记为需要重建。
            // 避免一帧内多次修改引发多次重建。
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);

            if (m_OnDirtyLayoutCallback != null)
                m_OnDirtyLayoutCallback();
        }

        /// <summary>
        /// 将顶点 (vertices) 标记为脏 (dirty)，表明其需要被重建。
        /// </summary>
        /// <remarks>
        /// 如果有任何元素注册了此回调，则发送 OnDirtyVertsCallback 通知。请参见 RegisterDirtyVerticesCallback。
        /// </remarks>
        public virtual void SetVerticesDirty()
        {
            if (!IsActive())
                return;

            // [重点注释 - 顶点重建]
            // 当 UI 的颜色 (Color) 或文本内容发生变化时，触发顶点脏标记。
            // 并将自身注册到 CanvasUpdateRegistry 的图形重建队列 (GraphicRebuildQueue) 中。
            m_VertsDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);

            if (m_OnDirtyVertsCallback != null)
                m_OnDirtyVertsCallback();
        }

        /// <summary>
        /// 将材质 (material) 标记为脏 (dirty)，表明其需要被重建。
        /// </summary>
        /// <remarks>
        /// 如果有任何元素注册了此回调，则发送 OnDirtyMaterialCallback 通知。请参见 RegisterDirtyMaterialCallback。
        /// </remarks>
        public virtual void SetMaterialDirty()
        {
            if (!IsActive())
                return;

            // [重点注释 - 材质重建]
            // 当 UI 的材质或贴图 (Texture) 发生变化时触发。
            // 同样注册到图形重建队列中，等待下一帧重绘。
            m_MaterialDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);

            if (m_OnDirtyMaterialCallback != null)
                m_OnDirtyMaterialCallback();
        }

        public void SetRaycastDirty()
        {
            if (m_RaycastTargetCache != m_RaycastTarget)
            {
                if (m_RaycastTarget && isActiveAndEnabled)
                    GraphicRegistry.RegisterRaycastGraphicForCanvas(canvas, this);

                else if (!m_RaycastTarget)
                    GraphicRegistry.UnregisterRaycastGraphicForCanvas(canvas, this);
            }
            m_RaycastTargetCache = m_RaycastTarget;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            if (gameObject.activeInHierarchy)
            {
                // prevent double dirtying...
                if (CanvasUpdateRegistry.IsRebuildingLayout())
                    SetVerticesDirty();
                else
                {
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        protected override void OnBeforeTransformParentChanged()
        {
            GraphicRegistry.UnregisterGraphicForCanvas(canvas, this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            m_Canvas = null;

            if (!IsActive())
                return;

            CacheCanvas();
            GraphicRegistry.RegisterGraphicForCanvas(canvas, this);
            SetAllDirty();
        }

        /// <summary>
        /// 图形的绝对深度（absolute depth），由渲染和事件系统使用 -- 从最低到最高。
        /// </summary>
        /// <example>
        /// 此深度是相对于第一个根 Canvas 而言的。
        ///
        /// Canvas
        ///  Graphic - 1
        ///  Graphic - 2
        ///  Nested Canvas (嵌套Canvas)
        ///     Graphic - 3
        ///     Graphic - 4
        ///  Graphic - 5
        ///
        /// 此值用于决定绘制和事件处理的顺序。
        /// </example>
        public int depth { get { return canvasRenderer.absoluteDepth; } }

        /// <summary>
        /// 此 Graphic 使用的 RectTransform 组件。已缓存以提高速度。
        /// </summary>
        public RectTransform rectTransform
        {
            get
            {
                // The RectTransform is a required component that must not be destroyed. Based on this assumption, a
                // null-reference check is sufficient.
                if (ReferenceEquals(m_RectTransform, null))
                {
                    m_RectTransform = GetComponent<RectTransform>();
                }
                return m_RectTransform;
            }
        }

        /// <summary>
        /// 此 Graphic 渲染到的目标 Canvas 的引用。
        /// </summary>
        /// <remarks>
        /// 在一个 Graphic 处于包含多个 Canvas 的层级结构中时，它将使用最靠近根节点的 Canvas。
        /// </remarks>
        public Canvas canvas
        {
            get
            {
                if (m_Canvas == null)
                    CacheCanvas();
                return m_Canvas;
            }
        }

        private void CacheCanvas()
        {
            var list = ListPool<Canvas>.Get();
            gameObject.GetComponentsInParent(false, list);
            if (list.Count > 0)
            {
                // Find the first active and enabled canvas.
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].isActiveAndEnabled)
                    {
                        m_Canvas = list[i];
                        break;
                    }

                    // if we reached the end and couldn't find an active and enabled canvas, we should return null . case 1171433
                    if (i == list.Count - 1)
                        m_Canvas = null;
                }
            }
            else
            {
                m_Canvas = null;
            }

            ListPool<Canvas>.Release(list);
        }

        /// <summary>
        /// 由此 Graphic 填充的 CanvasRenderer 引用。
        /// </summary>
        public CanvasRenderer canvasRenderer
        {
            get
            {
                // The CanvasRenderer is a required component that must not be destroyed. Based on this assumption, a
                // null-reference check is sufficient.
                if (ReferenceEquals(m_CanvasRenderer, null))
                {
                    m_CanvasRenderer = GetComponent<CanvasRenderer>();

                    if (ReferenceEquals(m_CanvasRenderer, null))
                    {
                        m_CanvasRenderer = gameObject.AddComponent<CanvasRenderer>();
                    }
                }
                return m_CanvasRenderer;
            }
        }

        /// <summary>
        /// 返回该图形的默认材质。
        /// </summary>
        public virtual Material defaultMaterial
        {
            get { return defaultGraphicMaterial; }
        }

        /// <summary>
        /// 用户设置的材质。
        /// </summary>
        public virtual Material material
        {
            get
            {
                return (m_Material != null) ? m_Material : defaultMaterial;
            }
            set
            {
                if (m_Material == value)
                    return;

                m_Material = value;
                SetMaterialDirty();
            }
        }

        /// <summary>
        /// 将被发送用于渲染的材质（只读）。
        /// </summary>
        /// <remarks>
        /// 这是实际发送到 CanvasRenderer 的材质。默认情况下，它与 [[Graphic.material]] 相同。
        /// 当扩展 Graphic 时，你可以重写此属性，以便将与 Graphic.material 不同的材质发送到 CanvasRenderer。
        /// 这在你想要以非破坏性方式修改用户设置的材质时非常有用。
        /// </remarks>
        public virtual Material materialForRendering
        {
            get
            {
                var components = ListPool<IMaterialModifier>.Get();
                GetComponents<IMaterialModifier>(components);

                var currentMat = material;
                for (var i = 0; i < components.Count; i++)
                    currentMat = (components[i] as IMaterialModifier).GetModifiedMaterial(currentMat);
                ListPool<IMaterialModifier>.Release(components);
                return currentMat;
            }
        }

        /// <summary>
        /// 图形的纹理 (只读)。
        /// </summary>
        /// <remarks>
        /// 这是一个会被传递给 CanvasRenderer、Material 以及着色器 _MainTex 的 Texture。
        ///
        /// 当实现你自己的 Graphic 时，你可以重写此属性，以控制哪个纹理流经 UI 渲染管线。
        ///
        /// 请记住，Unity 会尝试将 UI 元素批处理 (batch) 在一起以提高性能，因此最好使用图集 (atlas) 来减少 DrawCall 数量。
        /// </remarks>
        public virtual Texture mainTexture
        {
            get
            {
                return s_WhiteTexture;
            }
        }

        /// <summary>
        /// 标记 Graphic 和 Canvas 已经被改变。
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            CacheCanvas();
            GraphicRegistry.RegisterGraphicForCanvas(canvas, this);

#if UNITY_EDITOR
            GraphicRebuildTracker.TrackGraphic(this);
#endif
            if (s_WhiteTexture == null)
                s_WhiteTexture = Texture2D.whiteTexture;

            SetAllDirty();
        }

        /// <summary>
        /// Clear references.
        /// </summary>
        protected override void OnDisable()
        {
#if UNITY_EDITOR
            GraphicRebuildTracker.UnTrackGraphic(this);
#endif
            GraphicRegistry.DisableGraphicForCanvas(canvas, this);
            CanvasUpdateRegistry.DisableCanvasElementForRebuild(this);

            if (canvasRenderer != null)
                canvasRenderer.Clear();

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);

            base.OnDisable();
        }

        protected override void OnDestroy()
        {
#if UNITY_EDITOR
            GraphicRebuildTracker.UnTrackGraphic(this);
#endif
            GraphicRegistry.UnregisterGraphicForCanvas(canvas, this);
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
            if (m_CachedMesh)
                Destroy(m_CachedMesh);
            m_CachedMesh = null;

            base.OnDestroy();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            // Use m_Cavas so we dont auto call CacheCanvas
            Canvas currentCanvas = m_Canvas;

            // Clear the cached canvas. Will be fetched below if active.
            m_Canvas = null;

            if (!IsActive())
            {
                GraphicRegistry.UnregisterGraphicForCanvas(currentCanvas, this);
                return;
            }

            CacheCanvas();

            if (currentCanvas != m_Canvas)
            {
                GraphicRegistry.UnregisterGraphicForCanvas(currentCanvas, this);

                // Only register if we are active and enabled as OnCanvasHierarchyChanged can get called
                // during object destruction and we dont want to register ourself and then become null.
                if (IsActive())
                    GraphicRegistry.RegisterGraphicForCanvas(canvas, this);
            }
        }

        /// <summary>
        /// 当 <c>CanvasRenderer.cull</c> 被修改时必须调用此方法。
        /// </summary>
        /// <remarks>
        /// 这可用于执行之前因为 <c>Graphic</c> 被剔除 (culled) 而跳过的操作。
        /// </remarks>
        public virtual void OnCullingChanged()
        {
            if (!canvasRenderer.cull && (m_VertsDirty || m_MaterialDirty))
            {
                /// 当我们被剔除时，我们可能跳过了 <c>Rebuild</c> 的调用。
                CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
            }
        }

        /// <summary>
        /// 当 CanvasUpdateRegistry 触发对应的渲染阶段时被调用，用于根据当前脏标记执行重建。
        /// </summary>
        /// <param name="update">当前所处的 CanvasUpdate 渲染周期阶段。</param>
        /// <remarks>
        /// 有关 canvas 更新周期的更多详细信息，请参见 CanvasUpdateRegistry。
        /// </remarks>
        // [重点注释 - 图形重建核心方法]
        // 响应 CanvasUpdateRegistry 的遍历调用。
        // 它只关心 PreRender (渲染前) 阶段。在这个阶段：
        // 1. 如果 VertsDirty 为真，调用 UpdateGeometry() 重新生成并提交顶点到 CanvasRenderer。
        // 2. 如果 MaterialDirty 为真，调用 UpdateMaterial() 重新提交材质给 CanvasRenderer。
        // 真正产生耗时的是内部的 OnPopulateMesh 等重写方法。
        public virtual void Rebuild(CanvasUpdate update)
        {
            if (canvasRenderer == null || canvasRenderer.cull)
                return;

            switch (update)
            {
                case CanvasUpdate.PreRender:
                    if (m_VertsDirty)
                    {
                        UpdateGeometry();
                        m_VertsDirty = false;
                    }
                    if (m_MaterialDirty)
                    {
                        UpdateMaterial();
                        m_MaterialDirty = false;
                    }
                    break;
            }
        }

        public virtual void LayoutComplete()
        {}

        public virtual void GraphicUpdateComplete()
        {}

        /// <summary>
        /// 调用以将该 Graphic 的材质更新到 CanvasRenderer 上。
        /// </summary>
        protected virtual void UpdateMaterial()
        {
            if (!IsActive())
                return;

            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(materialForRendering, 0);
            canvasRenderer.SetTexture(mainTexture);
        }

        /// <summary>
        /// 调用以将该 Graphic 的几何网格更新到 CanvasRenderer 上。
        /// </summary>
        // [重点注释 - 顶点几何更新入口]
        // 渲染阶段的核心方法。它会调用 OnPopulateMesh 获取顶点数据，
        // 然后收集并执行挂载在该物体上的所有 IMeshModifier（比如 Outline、Shadow 等效果），
        // 最终通过 CanvasRenderer.SetMesh 提交给底层 C++ 渲染。
        protected virtual void UpdateGeometry()
        {
            if (useLegacyMeshGeneration)
            {
                DoLegacyMeshGeneration();
            }
            else
            {
                DoMeshGeneration();
            }
        }

        private void DoMeshGeneration()
        {
            if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
                OnPopulateMesh(s_VertexHelper);
            else
                s_VertexHelper.Clear(); // clear the vertex helper so invalid graphics dont draw.

            var components = ListPool<Component>.Get();
            GetComponents(typeof(IMeshModifier), components);

            for (var i = 0; i < components.Count; i++)
                ((IMeshModifier)components[i]).ModifyMesh(s_VertexHelper);

            ListPool<Component>.Release(components);

            s_VertexHelper.FillMesh(workerMesh);
            canvasRenderer.SetMesh(workerMesh);
        }

        private void DoLegacyMeshGeneration()
        {
            if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
            {
#pragma warning disable 618
                OnPopulateMesh(workerMesh);
#pragma warning restore 618
            }
            else
            {
                workerMesh.Clear();
            }

            var components = ListPool<Component>.Get();
            GetComponents(typeof(IMeshModifier), components);

            for (var i = 0; i < components.Count; i++)
            {
#pragma warning disable 618
                ((IMeshModifier)components[i]).ModifyMesh(workerMesh);
#pragma warning restore 618
            }

            ListPool<Component>.Release(components);
            canvasRenderer.SetMesh(workerMesh);
        }

        protected static Mesh workerMesh
        {
            get
            {
                if (s_Mesh == null)
                {
                    s_Mesh = new Mesh();
                    s_Mesh.name = "Shared UI Mesh";
                }
                return s_Mesh;
            }
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use OnPopulateMesh instead.", true)]
        protected virtual void OnFillVBO(System.Collections.Generic.List<UIVertex> vbo) {}

        [Obsolete("Use OnPopulateMesh(VertexHelper vh) instead.", false)]
        /// <summary>
        /// 当 UI 元素需要生成顶点时调用的回调函数。填充顶点缓冲区数据。
        /// </summary>
        /// <param name="m">需要用 UI 数据填充的 Mesh。</param>
        /// <remarks>
        /// 例如，由 Text、UI.Image 和 RawImage 用于生成其特定用例的顶点。
        /// </remarks>
        protected virtual void OnPopulateMesh(Mesh m)
        {
            OnPopulateMesh(s_VertexHelper);
            s_VertexHelper.FillMesh(m);
        }

        /// <summary>
        /// 当 UI 元素需要生成顶点时调用的回调函数。填充顶点缓冲区数据。
        /// </summary>
        /// <param name="vh">VertexHelper 辅助工具。</param>
        /// <remarks>
        /// 例如，由 Text、UI.Image 和 RawImage 用于生成其特定用例的顶点。
        /// </remarks>
        // [重点注释 - 网格生成核心重写方法]
        // 任何继承自 Graphic 的类（如 Image, Text, RawImage），都要重写这个方法，
        // 告诉 UGUI “我长什么样”。这里默认实现是画一个使用 color 属性颜色的四边形（两个三角形）。
        // 自定义形状的 UI（如雷达图、多边形头像）都是在这里利用 VertexHelper 添加顶点和三角形来实现的。
        protected virtual void OnPopulateMesh(VertexHelper vh)
        {
            var r = GetPixelAdjustedRect();
            var v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);

            Color32 color32 = color;
            vh.Clear();
            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(0f, 1f));
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(1f, 1f));
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(1f, 0f));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only callback that is issued by Unity if a rebuild of the Graphic is required.
        /// Currently sent when an asset is reimported.
        /// </summary>
        public virtual void OnRebuildRequested()
        {
            // when rebuild is requested we need to rebuild all the graphics /
            // and associated components... The correct way to do this is by
            // calling OnValidate... Because MB's don't have a common base class
            // we do this via reflection. It's nasty and ugly... Editor only.
            m_SkipLayoutUpdate = true;
            var mbs = gameObject.GetComponents<MonoBehaviour>();
            foreach (var mb in mbs)
            {
                if (mb == null)
                    continue;
                var methodInfo = mb.GetType().GetMethod("OnValidate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (methodInfo != null)
                    methodInfo.Invoke(mb, null);
            }
            m_SkipLayoutUpdate = false;
        }

        protected override void Reset()
        {
            SetAllDirty();
        }

#endif

        // Call from unity if animation properties have changed

        protected override void OnDidApplyAnimationProperties()
        {
            SetAllDirty();
        }

        /// <summary>
        /// 使 Graphic 的大小适应其内容的原生大小。
        /// </summary>
        public virtual void SetNativeSize() {}

        /// <summary>
        /// 当 GraphicRaycaster 在场景中进行射线检测时，它会执行两项操作。首先，它会利用元素的 RectTransform 的 Rect 来筛选元素。然后，它使用此 Raycast 函数来确定哪些元素实际被射线击中。
        /// </summary>
        /// <param name="sp">正在测试的屏幕坐标点 (Screen point)</param>
        /// <param name="eventCamera">用于测试的相机。</param>
        /// <returns>如果提供的点对于 GraphicRaycaster 射线检测是一个有效的位置，则返回 True。</returns>
        // [重点注释 - 射线检测精确判断]
        // 这一步发生在 GraphicRaycaster 已经用 RectTransform 边框完成粗筛之后。
        // 它会向上遍历组件树，检查是否有任何 ICanvasRaycastFilter（例如 Image 或 RaycastMask）阻止了这次点击。
        // 如果遇到 CanvasGroup 且 ignoreParentGroups 为 true，则会停止向上遍历。
        public virtual bool Raycast(Vector2 sp, Camera eventCamera)
        {
            if (!isActiveAndEnabled)
                return false;

            var t = transform;
            var components = ListPool<Component>.Get();

            bool ignoreParentGroups = false;
            bool continueTraversal = true;

            while (t != null)
            {
                t.GetComponents(components);
                for (var i = 0; i < components.Count; i++)
                {
                    var canvas = components[i] as Canvas;
                    if (canvas != null && canvas.overrideSorting)
                        continueTraversal = false;

                    var filter = components[i] as ICanvasRaycastFilter;

                    if (filter == null)
                        continue;

                    var raycastValid = true;

                    var group = components[i] as CanvasGroup;
                    if (group != null)
                    {
                        if (!group.enabled)
                            continue;

                        if (ignoreParentGroups == false && group.ignoreParentGroups)
                        {
                            ignoreParentGroups = true;
                            raycastValid = filter.IsRaycastLocationValid(sp, eventCamera);
                        }
                        else if (!ignoreParentGroups)
                            raycastValid = filter.IsRaycastLocationValid(sp, eventCamera);
                    }
                    else
                    {
                        raycastValid = filter.IsRaycastLocationValid(sp, eventCamera);
                    }

                    if (!raycastValid)
                    {
                        ListPool<Component>.Release(components);
                        return false;
                    }
                }
                t = continueTraversal ? t.parent : null;
            }
            ListPool<Component>.Release(components);
            return true;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetAllDirty();
        }

#endif

        ///<summary>
        /// 调整给定的像素以达到像素完美（Pixel Perfect）。
        ///</summary>
        ///<param name="point">局部空间点。</param>
        ///<returns>经过像素完美调整后的点。</returns>
        ///<remarks>
        ///注意：仅当 Graphic 的根 Canvas 处于屏幕空间 (Screen Space) 时，此功能才准确。
        ///</remarks>
        public Vector2 PixelAdjustPoint(Vector2 point)
        {
            if (!canvas || canvas.renderMode == RenderMode.WorldSpace || canvas.scaleFactor == 0.0f || !canvas.pixelPerfect)
                return point;
            else
            {
                return RectTransformUtility.PixelAdjustPoint(point, transform, canvas);
            }
        }

        /// <summary>
        /// 返回最接近 Graphic 的 RectTransform 的像素完美 Rect。
        /// </summary>
        /// <remarks>
        /// 注意：仅当 Graphic 的根 Canvas 处于屏幕空间 (Screen Space) 时，此功能才准确。
        /// </remarks>
        /// <returns>像素完美的 Rect。</returns>
        public Rect GetPixelAdjustedRect()
        {
            if (!canvas || canvas.renderMode == RenderMode.WorldSpace || canvas.scaleFactor == 0.0f || !canvas.pixelPerfect)
                return rectTransform.rect;
            else
                return RectTransformUtility.PixelAdjustRect(rectTransform, canvas);
        }

        ///<summary>
        /// 使用动画渐变 (Tween) 与此 Graphic 关联的 CanvasRenderer 的颜色。
        ///</summary>
        ///<param name="targetColor">目标颜色。</param>
        ///<param name="duration">渐变持续时间。</param>
        ///<param name="ignoreTimeScale">是否应忽略 Time.scale 带来的时间缩放？</param>
        ///<param name="useAlpha">是否也应渐变 Alpha 通道？</param>
        public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha)
        {
            CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha, true);
        }

        ///<summary>
        /// 使用动画渐变 (Tween) 与此 Graphic 关联的 CanvasRenderer 的颜色。
        ///</summary>
        ///<param name="targetColor">目标颜色。</param>
        ///<param name="duration">渐变持续时间。</param>
        ///<param name="ignoreTimeScale">是否应忽略 Time.scale 带来的时间缩放？</param>
        ///<param name="useAlpha">是否也应渐变 Alpha 通道？</param>
        /// <param name="useRGB">应该使用颜色本身还是 alpha 进行渐变</param>
        public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha, bool useRGB)
        {
            if (canvasRenderer == null || (!useRGB && !useAlpha))
                return;

            Color currentColor = canvasRenderer.GetColor();
            if (currentColor.Equals(targetColor))
            {
                m_ColorTweenRunner.StopTween();
                return;
            }

            ColorTween.ColorTweenMode mode = (useRGB && useAlpha ?
                ColorTween.ColorTweenMode.All :
                (useRGB ? ColorTween.ColorTweenMode.RGB : ColorTween.ColorTweenMode.Alpha));

            var colorTween = new ColorTween {duration = duration, startColor = canvasRenderer.GetColor(), targetColor = targetColor};
            colorTween.AddOnChangedCallback(canvasRenderer.SetColor);
            colorTween.ignoreTimeScale = ignoreTimeScale;
            colorTween.tweenMode = mode;
            m_ColorTweenRunner.StartTween(colorTween);
        }

        static private Color CreateColorFromAlpha(float alpha)
        {
            var alphaColor = Color.black;
            alphaColor.a = alpha;
            return alphaColor;
        }

        ///<summary>
        /// 使用动画渐变 (Tween) 与此 Graphic 关联的 CanvasRenderer 颜色的 Alpha 值。
        ///</summary>
        ///<param name="alpha">目标 Alpha 值。</param>
        ///<param name="duration">渐变持续时间（以秒为单位）。</param>
        ///<param name="ignoreTimeScale">是否应忽略 [[Time.scale]] 带来的时间缩放？</param>
        public virtual void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale)
        {
            CrossFadeColor(CreateColorFromAlpha(alpha), duration, ignoreTimeScale, true, false);
        }

        /// <summary>
        /// 添加一个监听器，以便在图形布局 (layout) 被标记为脏时接收通知。
        /// </summary>
        /// <param name="action">被调用时执行的方法。</param>
        public void RegisterDirtyLayoutCallback(UnityAction action)
        {
            m_OnDirtyLayoutCallback += action;
        }

        /// <summary>
        /// 移除一个监听器，停止接收图形布局被标记为脏时的通知。
        /// </summary>
        /// <param name="action">被调用时执行的方法。</param>
        public void UnregisterDirtyLayoutCallback(UnityAction action)
        {
            m_OnDirtyLayoutCallback -= action;
        }

        /// <summary>
        /// 添加一个监听器，以便在图形顶点 (vertices) 被标记为脏时接收通知。
        /// </summary>
        /// <param name="action">被调用时执行的方法。</param>
        public void RegisterDirtyVerticesCallback(UnityAction action)
        {
            m_OnDirtyVertsCallback += action;
        }

        /// <summary>
        /// 移除一个监听器，停止接收图形顶点被标记为脏时的通知。
        /// </summary>
        /// <param name="action">被调用时执行的方法。</param>
        public void UnregisterDirtyVerticesCallback(UnityAction action)
        {
            m_OnDirtyVertsCallback -= action;
        }

        /// <summary>
        /// 添加一个监听器，以便在图形材质 (material) 被标记为脏时接收通知。
        /// </summary>
        /// <param name="action">被调用时执行的方法。</param>
        public void RegisterDirtyMaterialCallback(UnityAction action)
        {
            m_OnDirtyMaterialCallback += action;
        }

        /// <summary>
        /// 移除一个监听器，停止接收图形材质被标记为脏时的通知。
        /// </summary>
        /// <param name="action">被调用时执行的方法。</param>
        public void UnregisterDirtyMaterialCallback(UnityAction action)
        {
            m_OnDirtyMaterialCallback -= action;
        }
    }
}
