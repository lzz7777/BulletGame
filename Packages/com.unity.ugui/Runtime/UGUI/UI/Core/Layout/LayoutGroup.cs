using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// 供布局组 (Layout Groups) 使用的抽象基类。
    /// </summary>
    // [重点注释 - 自动布局基类]
    // 所有的内置布局组件 (Horizontal/Vertical/GridLayoutGroup) 都继承自它。
    // 它实现了 ILayoutElement（提供自身的宽高需求给父节点）和 ILayoutGroup（负责排版子节点）。
    public abstract class LayoutGroup : UIBehaviour, ILayoutElement, ILayoutGroup
    {
        [SerializeField] protected RectOffset m_Padding = new RectOffset();

        /// <summary>
        /// 要在子布局元素周围添加的内边距 (padding)。
        /// </summary>
        public RectOffset padding { get { return m_Padding; } set { SetProperty(ref m_Padding, value); } }

        [SerializeField] protected TextAnchor m_ChildAlignment = TextAnchor.UpperLeft;

        /// <summary>
        /// 布局组中子布局元素所使用的对齐方式。
        /// </summary>
        /// <remarks>
        /// 如果布局元素没有指定 flexible 宽度或高度，其子元素可能无法完全填满布局组内的可用空间。在这种情况下，使用对齐设置来指定子元素在其布局组内的对齐方式。
        /// </remarks>
        public TextAnchor childAlignment { get { return m_ChildAlignment; } set { SetProperty(ref m_ChildAlignment, value); } }

        [System.NonSerialized] private RectTransform m_Rect;
        protected RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        protected DrivenRectTransformTracker m_Tracker;
        private Vector2 m_TotalMinSize = Vector2.zero;
        private Vector2 m_TotalPreferredSize = Vector2.zero;
        private Vector2 m_TotalFlexibleSize = Vector2.zero;

        [System.NonSerialized] private List<RectTransform> m_RectChildren = new List<RectTransform>();
        protected List<RectTransform> rectChildren { get { return m_RectChildren; } }

        // [重点注释 - 布局计算核心]
        // 这是自动布局 4 步走的第一步：收集所有需要排版的子节点。
        // 它会忽略处于非激活状态的，或者挂载了 ILayoutIgnorer（如 LayoutElement 的 ignoreLayout=true）的子节点。
        public virtual void CalculateLayoutInputHorizontal()
        {
            m_RectChildren.Clear();
            var toIgnoreList = ListPool<Component>.Get();
            for (int i = 0; i < rectTransform.childCount; i++)
            {
                var rect = rectTransform.GetChild(i) as RectTransform;
                if (rect == null || !rect.gameObject.activeInHierarchy)
                    continue;

                rect.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);

                if (toIgnoreList.Count == 0)
                {
                    m_RectChildren.Add(rect);
                    continue;
                }

                for (int j = 0; j < toIgnoreList.Count; j++)
                {
                    var ignorer = (ILayoutIgnorer)toIgnoreList[j];
                    if (!ignorer.ignoreLayout)
                    {
                        m_RectChildren.Add(rect);
                        break;
                    }
                }
            }
            ListPool<Component>.Release(toIgnoreList);
            m_Tracker.Clear();
        }

        public abstract void CalculateLayoutInputVertical();

        /// <summary>
        /// See LayoutElement.minWidth
        /// </summary>
        public virtual float minWidth { get { return GetTotalMinSize(0); } }

        /// <summary>
        /// See LayoutElement.preferredWidth
        /// </summary>
        public virtual float preferredWidth { get { return GetTotalPreferredSize(0); } }

        /// <summary>
        /// See LayoutElement.flexibleWidth
        /// </summary>
        public virtual float flexibleWidth { get { return GetTotalFlexibleSize(0); } }

        /// <summary>
        /// See LayoutElement.minHeight
        /// </summary>
        public virtual float minHeight { get { return GetTotalMinSize(1); } }

        /// <summary>
        /// See LayoutElement.preferredHeight
        /// </summary>
        public virtual float preferredHeight { get { return GetTotalPreferredSize(1); } }

        /// <summary>
        /// See LayoutElement.flexibleHeight
        /// </summary>
        public virtual float flexibleHeight { get { return GetTotalFlexibleSize(1); } }

        /// <summary>
        /// See LayoutElement.layoutPriority
        /// </summary>
        public virtual int layoutPriority { get { return 0; } }

        // ILayoutController Interface

        public abstract void SetLayoutHorizontal();
        public abstract void SetLayoutVertical();

        // Implementation

        protected LayoutGroup()
        {
            if (m_Padding == null)
                m_Padding = new RectOffset();
        }

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

        /// <summary>
        /// 当属性被动画 (animation) 改变时的回调。
        /// </summary>
        protected override void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

        /// <summary>
        /// 给定轴上布局组的最小尺寸 (min size)。
        /// </summary>
        /// <param name="axis">轴的索引。0 是水平 (horizontal)，1 是垂直 (vertical)。</param>
        /// <returns>最小尺寸 (min size)</returns>
        protected float GetTotalMinSize(int axis)
        {
            return m_TotalMinSize[axis];
        }

        /// <summary>
        /// 给定轴上布局组的首选尺寸 (preferred size)。
        /// </summary>
        /// <param name="axis">轴的索引。0 是水平 (horizontal)，1 是垂直 (vertical)。</param>
        /// <returns>首选尺寸 (preferred size)。</returns>
        protected float GetTotalPreferredSize(int axis)
        {
            return m_TotalPreferredSize[axis];
        }

        /// <summary>
        /// 给定轴上布局组的灵活尺寸 (flexible size)。
        /// </summary>
        /// <param name="axis">轴的索引。0 是水平 (horizontal)，1 是垂直 (vertical)。</param>
        /// <returns>灵活尺寸 (flexible size)</returns>
        protected float GetTotalFlexibleSize(int axis)
        {
            return m_TotalFlexibleSize[axis];
        }

        /// <summary>
        /// 返回第一个子布局元素沿给定轴的计算位置。
        /// </summary>
        /// <param name="axis">轴的索引。0 是水平 (horizontal)，1 是垂直 (vertical)。</param>
        /// <param name="requiredSpaceWithoutPadding">所有布局元素在给定轴上所需的总空间，包含间距 (spacing)，但不包含内边距 (padding)。</param>
        /// <returns>第一个子元素沿给定轴的位置。</returns>
        protected float GetStartOffset(int axis, float requiredSpaceWithoutPadding)
        {
            float requiredSpace = requiredSpaceWithoutPadding + (axis == 0 ? padding.horizontal : padding.vertical);
            float availableSpace = rectTransform.rect.size[axis];
            float surplusSpace = availableSpace - requiredSpace;
            float alignmentOnAxis = GetAlignmentOnAxis(axis);
            return (axis == 0 ? padding.left : padding.top) + surplusSpace * alignmentOnAxis;
        }

        /// <summary>
        /// 以小数形式返回指定轴上的对齐方式，其中 0 表示左/上，0.5 表示居中，1 表示右/下。
        /// </summary>
        /// <param name="axis">获取对齐方式的轴。0 是水平 (horizontal)，1 是垂直 (vertical)。</param>
        /// <returns>以小数形式表示的对齐方式，其中 0 表示左/上，0.5 表示居中，1 表示右/下。</returns>
        protected float GetAlignmentOnAxis(int axis)
        {
            if (axis == 0)
                return ((int)childAlignment % 3) * 0.5f;
            else
                return ((int)childAlignment / 3) * 0.5f;
        }

        /// <summary>
        /// 用于设置给定轴的计算布局属性。
        /// </summary>
        /// <param name="totalMin">布局组的最小尺寸 (min size)。</param>
        /// <param name="totalPreferred">布局组的首选尺寸 (preferred size)。</param>
        /// <param name="totalFlexible">布局组的灵活尺寸 (flexible size)。</param>
        /// <param name="axis">设置尺寸的轴。0 是水平 (horizontal)，1 是垂直 (vertical)。</param>
        protected void SetLayoutInputForAxis(float totalMin, float totalPreferred, float totalFlexible, int axis)
        {
            m_TotalMinSize[axis] = totalMin;
            m_TotalPreferredSize[axis] = totalPreferred;
            m_TotalFlexibleSize[axis] = totalFlexible;
        }

        /// <summary>
        /// 沿给定轴设置子布局元素的位置和尺寸。
        /// </summary>
        /// <param name="rect">子布局元素的 RectTransform。</param>
        /// <param name="axis">设置位置和尺寸的轴。0 是水平 (horizontal)，1 是垂直 (vertical)。</param>
        /// <param name="pos">距左侧或顶部的位置。</param>
        protected void SetChildAlongAxis(RectTransform rect, int axis, float pos)
        {
            if (rect == null)
                return;

            SetChildAlongAxisWithScale(rect, axis, pos, 1.0f);
        }

        /// <summary>
        /// 沿给定轴设置子布局元素的位置和尺寸，并应用缩放因子。
        /// </summary>
        /// <param name="rect">子布局元素的 RectTransform。</param>
        /// <param name="axis">设置位置和尺寸的轴。0 是水平 (horizontal)，1 是垂直 (vertical)。</param>
        /// <param name="pos">距左侧或顶部的位置。</param>
        /// <param name="scaleFactor">缩放因子。</param>
        protected void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, float scaleFactor)
        {
            if (rect == null)
                return;

            m_Tracker.Add(this, rect,
                DrivenTransformProperties.Anchors |
                (axis == 0 ? DrivenTransformProperties.AnchoredPositionX : DrivenTransformProperties.AnchoredPositionY));

            // 内联了 rect.SetInsetAndSizeFromParentEdge(...) 并重构了代码，以便将所需尺寸乘以缩放因子 scaleFactor。
            // sizeDelta 必须保持不变，但在计算位置时使用的尺寸必须乘以缩放因子 scaleFactor。

            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;

            Vector2 anchoredPosition = rect.anchoredPosition;
            anchoredPosition[axis] = (axis == 0) ? (pos + rect.sizeDelta[axis] * rect.pivot[axis] * scaleFactor) : (-pos - rect.sizeDelta[axis] * (1f - rect.pivot[axis]) * scaleFactor);
            rect.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// 沿给定轴设置子布局元素的位置和尺寸。
        /// </summary>
        /// <param name="rect">子布局元素的 RectTransform。</param>
        /// <param name="axis">设置位置和尺寸的轴。0 是水平 (horizontal)，1 是垂直 (vertical)。</param>
        /// <param name="pos">距左侧或顶部的位置。</param>
        /// <param name="size">尺寸大小。</param>
        protected void SetChildAlongAxis(RectTransform rect, int axis, float pos, float size)
        {
            if (rect == null)
                return;

            SetChildAlongAxisWithScale(rect, axis, pos, size, 1.0f);
        }

        /// <summary>
        /// 沿给定轴设置子布局元素的位置和尺寸，并应用缩放因子。
        /// </summary>
        /// <param name="rect">子布局元素的 RectTransform。</param>
        /// <param name="axis">设置位置和尺寸的轴。0 是水平 (horizontal)，1 是垂直 (vertical)。</param>
        /// <param name="pos">距左侧或顶部的位置。</param>
        /// <param name="size">尺寸大小。</param>
        /// <param name="scaleFactor">缩放因子。</param>
        protected void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, float size, float scaleFactor)
        {
            if (rect == null)
                return;

            m_Tracker.Add(this, rect,
                DrivenTransformProperties.Anchors |
                (axis == 0 ?
                    (DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.SizeDeltaX) :
                    (DrivenTransformProperties.AnchoredPositionY | DrivenTransformProperties.SizeDeltaY)
                )
            );

            // 内联了 rect.SetInsetAndSizeFromParentEdge(...) 并重构了代码，以便将所需尺寸乘以缩放因子 scaleFactor。
            // sizeDelta 必须保持不变，但在计算位置时使用的尺寸必须乘以缩放因子 scaleFactor。

            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;

            Vector2 sizeDelta = rect.sizeDelta;
            sizeDelta[axis] = size;
            rect.sizeDelta = sizeDelta;

            Vector2 anchoredPosition = rect.anchoredPosition;
            anchoredPosition[axis] = (axis == 0) ? (pos + size * rect.pivot[axis] * scaleFactor) : (-pos - size * (1f - rect.pivot[axis]) * scaleFactor);
            rect.anchoredPosition = anchoredPosition;
        }

        private bool isRootLayoutGroup
        {
            get
            {
                Transform parent = transform.parent;
                if (parent == null)
                    return true;
                return transform.parent.GetComponent(typeof(ILayoutGroup)) == null;
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (isRootLayoutGroup)
                SetDirty();
        }

        protected virtual void OnTransformChildrenChanged()
        {
            SetDirty();
        }

        /// <summary>
        /// 用于设置给定属性（如果它已发生改变）的辅助方法。
        /// </summary>
        /// <param name="currentValue">成员值的引用。</param>
        /// <param name="newValue">新值。</param>
        protected void SetProperty<T>(ref T currentValue, T newValue)
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return;
            currentValue = newValue;
            SetDirty();
        }

        /// <summary>
        /// 将 LayoutGroup 标记为脏 (dirty)。
        /// </summary>
        protected void SetDirty()
        {
            if (!IsActive())
                return;

            if (!CanvasUpdateRegistry.IsRebuildingLayout())
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            else
                StartCoroutine(DelayedSetDirty(rectTransform));
        }

        IEnumerator DelayedSetDirty(RectTransform rectTransform)
        {
            yield return null;
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
