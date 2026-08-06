using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/Content Size Fitter", 141)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// 调整 RectTransform 的大小以适应其内容的大小。
    /// </summary>
    /// <remarks>
    /// ContentSizeFitter 可以用于包含一个或多个 ILayoutElement 组件的 GameObject 上，例如 Text、Image、HorizontalLayoutGroup、VerticalLayoutGroup 和 GridLayoutGroup。
    /// </remarks>
    // [重点注释 - 内容尺寸适配器]
    // 它实现了 ILayoutSelfController，专门用来控制自身的大小。
    // 在 LayoutRebuilder 的四步遍历中，它会在控制阶段 (SetLayoutHorizontal/Vertical) 被调用。
    // 它会通过 LayoutUtility 去获取子级提供的 minSize 或 preferredSize，并据此修改自身的尺寸。
    // 如果一个包含大量文本的 Text 挂载了它，文本的任何变化都会导致它重新计算大小并向上传递脏标记。
    public class ContentSizeFitter : UIBehaviour, ILayoutSelfController
    {
        /// <summary>
        /// 可用的尺寸适配模式。
        /// </summary>
        public enum FitMode
        {
            /// <summary>
            /// 不执行任何大小调整。
            /// </summary>
            Unconstrained,
            /// <summary>
            /// 调整为内容的最小尺寸 (minimum size)。
            /// </summary>
            MinSize,
            /// <summary>
            /// 调整为内容的首选尺寸 (preferred size)。
            /// </summary>
            PreferredSize
        }

        [SerializeField] protected FitMode m_HorizontalFit = FitMode.Unconstrained;

        /// <summary>
        /// 用于确定宽度的适配模式。
        /// </summary>
        public FitMode horizontalFit { get { return m_HorizontalFit; } set { if (SetPropertyUtility.SetStruct(ref m_HorizontalFit, value)) SetDirty(); } }

        [SerializeField] protected FitMode m_VerticalFit = FitMode.Unconstrained;

        /// <summary>
        /// 用于确定高度的适配模式。
        /// </summary>
        public FitMode verticalFit { get { return m_VerticalFit; } set { if (SetPropertyUtility.SetStruct(ref m_VerticalFit, value)) SetDirty(); } }

        [System.NonSerialized] private RectTransform m_Rect;
        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        // field is never assigned warning
        #pragma warning disable 649
        private DrivenRectTransformTracker m_Tracker;
        #pragma warning restore 649

        protected ContentSizeFitter()
        {}

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        private void HandleSelfFittingAlongAxis(int axis)
        {
            FitMode fitting = (axis == 0 ? horizontalFit : verticalFit);
            if (fitting == FitMode.Unconstrained)
            {
                // 保留对被跟踪的 Transform 的引用，但不控制其属性：
                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.None);
                return;
            }

            m_Tracker.Add(this, rectTransform, (axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY));

            // 将大小设置为最小尺寸或首选尺寸
            if (fitting == FitMode.MinSize)
                rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, LayoutUtility.GetMinSize(m_Rect, axis));
            else
                rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, LayoutUtility.GetPreferredSize(m_Rect, axis));
        }

        /// <summary>
        /// 计算并应用尺寸的水平分量到 RectTransform
        /// </summary>
        public virtual void SetLayoutHorizontal()
        {
            m_Tracker.Clear();
            HandleSelfFittingAlongAxis(0);
        }

        /// <summary>
        /// 计算并应用尺寸的垂直分量到 RectTransform
        /// </summary>
        public virtual void SetLayoutVertical()
        {
            HandleSelfFittingAlongAxis(1);
        }

        protected void SetDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirty();
        }

    #endif
    }
}
