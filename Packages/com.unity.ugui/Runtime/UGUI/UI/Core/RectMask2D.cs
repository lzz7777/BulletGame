using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Rect Mask 2D", 14)]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// 一种二维的矩形遮罩，可限制其中 UI 元素的显示区域。
    /// </summary>
    /// <remarks>
    /// RectMask2D 提供与标准 Mask 组件类似的遮罩功能，但存在一些限制并具备显著的性能优势：
    /// * 仅支持二维（2D）矩形形状。
    /// * 要求遮罩上的元素位于同一平面。
    /// * 不依赖模板缓冲区 (stencil buffer) / 不产生额外的 DrawCall。
    /// * 所需的 DrawCall 较少。
    /// * 会将遮罩区域外的元素剔除 (Cull) 不予渲染。
    /// </remarks>
    // [重点注释 - 数学裁剪遮罩]
    // 它是如何不增加 DrawCall 做到裁剪的？
    // 核心原理：它利用 IClipper 接口在 CanvasUpdateRegistry 的 Culling 阶段，
    // 把裁剪区域（Rect）传给所有子级的 MaskableGraphic。子级在提交网格前，直接通过着色器参数或顶点截断的方式将外围像素抛弃。
    // 这对于性能极其友好，所以列表类型的 UI (ScrollView) 强烈建议使用 RectMask2D。
    public class RectMask2D : UIBehaviour, IClipper, ICanvasRaycastFilter
    {
        [NonSerialized]
        private readonly RectangularVertexClipper m_VertexClipper = new RectangularVertexClipper();

        [NonSerialized]
        private RectTransform m_RectTransform;

        [NonSerialized]
        private HashSet<MaskableGraphic> m_MaskableTargets = new HashSet<MaskableGraphic>();

        [NonSerialized]
        private HashSet<IClippable> m_ClipTargets = new HashSet<IClippable>();

        [NonSerialized]
        private bool m_ShouldRecalculateClipRects;

        [NonSerialized]
        private List<RectMask2D> m_Clippers = new List<RectMask2D>();

        [NonSerialized]
        private Rect m_LastClipRectCanvasSpace;
        [NonSerialized]
        private bool m_ForceClip;

        [SerializeField]
        private Vector4 m_Padding = new Vector4();

        /// <summary>
        /// Padding to be applied to the masking
        /// X = Left
        /// Y = Bottom
        /// Z = Right
        /// W = Top
        /// </summary>
        public Vector4 padding
        {
            get { return m_Padding; }
            set
            {
                m_Padding = value;
                MaskUtilities.Notify2DMaskStateChanged(this);
            }
        }

        [SerializeField]
        private Vector2Int m_Softness;

        /// <summary>
        /// The softness to apply to the horizontal and vertical axis.
        /// </summary>
        public Vector2Int softness
        {
            get { return m_Softness;  }
            set
            {
                m_Softness.x = Mathf.Max(0, value.x);
                m_Softness.y = Mathf.Max(0, value.y);
                MaskUtilities.Notify2DMaskStateChanged(this);
            }
        }

        /// <remarks>
        /// Returns a non-destroyed instance or a null reference.
        /// </remarks>
        [NonSerialized] private Canvas m_Canvas;
        internal Canvas Canvas
        {
            get
            {
                if (m_Canvas == null)
                {
                    var list = ListPool<Canvas>.Get();
                    gameObject.GetComponentsInParent(false, list);
                    if (list.Count > 0)
                        m_Canvas = list[list.Count - 1];
                    else
                        m_Canvas = null;
                    ListPool<Canvas>.Release(list);
                }

                return m_Canvas;
            }
        }

        /// <summary>
        /// Get the Rect for the mask in canvas space.
        /// </summary>
        public Rect canvasRect
        {
            get
            {
                return m_VertexClipper.GetCanvasRect(rectTransform, Canvas);
            }
        }

        /// <summary>
        /// Helper function to get the RectTransform for the mask.
        /// </summary>
        public RectTransform rectTransform
        {
            get { return m_RectTransform ?? (m_RectTransform = GetComponent<RectTransform>()); }
        }

        protected RectMask2D()
        {}

        protected override void OnEnable()
        {
            base.OnEnable();
            m_ShouldRecalculateClipRects = true;
            ClipperRegistry.Register(this);
            MaskUtilities.Notify2DMaskStateChanged(this);
        }

        protected override void OnDisable()
        {
            // we call base OnDisable first here
            // as we need to have the IsActive return the
            // correct value when we notify the children
            // that the mask state has changed.
            base.OnDisable();
            m_ClipTargets.Clear();
            m_MaskableTargets.Clear();
            m_Clippers.Clear();
            ClipperRegistry.Disable(this);
            MaskUtilities.Notify2DMaskStateChanged(this);
        }

        protected override void OnDestroy()
        {
            ClipperRegistry.Unregister(this);
            base.OnDestroy();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_ShouldRecalculateClipRects = true;

            // Dont allow negative softness.
            m_Softness.x = Mathf.Max(0, m_Softness.x);
            m_Softness.y = Mathf.Max(0, m_Softness.y);

            if (!IsActive())
                return;

            MaskUtilities.Notify2DMaskStateChanged(this);
        }

#endif

        public virtual bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (!isActiveAndEnabled)
                return true;

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera, m_Padding);
        }

        private Vector3[] m_Corners = new Vector3[4];

        private Rect rootCanvasRect
        {
            get
            {
                rectTransform.GetWorldCorners(m_Corners);

                if (!ReferenceEquals(Canvas, null))
                {
                    Canvas rootCanvas = Canvas.rootCanvas;
                    for (int i = 0; i < 4; ++i)
                        m_Corners[i] = rootCanvas.transform.InverseTransformPoint(m_Corners[i]);
                }

                return new Rect(m_Corners[0].x, m_Corners[0].y, m_Corners[2].x - m_Corners[0].x, m_Corners[2].y - m_Corners[0].y);
            }
        }

        public virtual void PerformClipping()
        {
            if (ReferenceEquals(Canvas, null))
            {
                return;
            }

            //TODO See if an IsActive() test would work well here or whether it might cause unexpected side effects (re case 776771)

            // if the parents are changed
            // or something similar we
            // do a recalculate here
            if (m_ShouldRecalculateClipRects)
            {
                MaskUtilities.GetRectMasksForClip(this, m_Clippers);
                m_ShouldRecalculateClipRects = false;
            }

            // get the compound rects from
            // the clippers that are valid
            bool validRect = true;
            Rect clipRect = Clipping.FindCullAndClipWorldRect(m_Clippers, out validRect);

            // If the mask is in ScreenSpaceOverlay/Camera render mode, its content is only rendered when its rect
            // overlaps that of the root canvas.
            RenderMode renderMode = Canvas.rootCanvas.renderMode;
            bool maskIsCulled =
                (renderMode == RenderMode.ScreenSpaceCamera || renderMode == RenderMode.ScreenSpaceOverlay) &&
                !clipRect.Overlaps(rootCanvasRect, true);

            if (maskIsCulled)
            {
                // Children are only displayed when inside the mask. If the mask is culled, then the children
                // inside the mask are also culled. In that situation, we pass an invalid rect to allow callees
                // to avoid some processing.
                clipRect = Rect.zero;
                validRect = false;
            }

            if (clipRect != m_LastClipRectCanvasSpace)
            {
                foreach (IClippable clipTarget in m_ClipTargets)
                {
                    clipTarget.SetClipRect(clipRect, validRect);
                }

                foreach (MaskableGraphic maskableTarget in m_MaskableTargets)
                {
                    maskableTarget.SetClipRect(clipRect, validRect);
                    maskableTarget.Cull(clipRect, validRect);
                }
            }
            else if (m_ForceClip)
            {
                foreach (IClippable clipTarget in m_ClipTargets)
                {
                    clipTarget.SetClipRect(clipRect, validRect);
                }

                foreach (MaskableGraphic maskableTarget in m_MaskableTargets)
                {
                    maskableTarget.SetClipRect(clipRect, validRect);

                    if (maskableTarget.canvasRenderer.hasMoved)
                        maskableTarget.Cull(clipRect, validRect);
                }
            }
            else
            {
                foreach (MaskableGraphic maskableTarget in m_MaskableTargets)
                {
                    //Case 1170399 - hasMoved is not a valid check when animating on pivot of the object
                    maskableTarget.Cull(clipRect, validRect);
                }
            }

            m_LastClipRectCanvasSpace = clipRect;
            m_ForceClip = false;

            UpdateClipSoftness();
        }

        public virtual void UpdateClipSoftness()
        {
            if (ReferenceEquals(Canvas, null))
            {
                return;
            }

            foreach (IClippable clipTarget in m_ClipTargets)
            {
                clipTarget.SetClipSoftness(m_Softness);
            }

            foreach (MaskableGraphic maskableTarget in m_MaskableTargets)
            {
                maskableTarget.SetClipSoftness(m_Softness);
            }
        }

        /// <summary>
        /// Add a IClippable to be tracked by the mask.
        /// </summary>
        /// <param name="clippable">Add the clippable object for this mask</param>
        public void AddClippable(IClippable clippable)
        {
            if (clippable == null)
                return;
            m_ShouldRecalculateClipRects = true;
            MaskableGraphic maskable = clippable as MaskableGraphic;

            if (maskable == null)
                m_ClipTargets.Add(clippable);
            else
                m_MaskableTargets.Add(maskable);

            m_ForceClip = true;
        }

        /// <summary>
        /// Remove an IClippable from being tracked by the mask.
        /// </summary>
        /// <param name="clippable">Remove the clippable object from this mask</param>
        public void RemoveClippable(IClippable clippable)
        {
            if (clippable == null)
                return;

            m_ShouldRecalculateClipRects = true;
            clippable.SetClipRect(new Rect(), false);

            MaskableGraphic maskable = clippable as MaskableGraphic;

            if (maskable == null)
                m_ClipTargets.Remove(clippable);
            else
                m_MaskableTargets.Remove(maskable);

            m_ForceClip = true;
        }

        protected override void OnTransformParentChanged()
        {
            m_Canvas = null;
            base.OnTransformParentChanged();
            m_ShouldRecalculateClipRects = true;
        }

        protected override void OnCanvasHierarchyChanged()
        {
            m_Canvas = null;
            base.OnCanvasHierarchyChanged();
            m_ShouldRecalculateClipRects = true;
        }
    }
}
