using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    /// <summary>
    /// 用于管理和封装 CanvasElement 布局重建的类。
    /// </summary>
    // [重点注释 - 自动布局管理类]
    // 它是整个 UI 自动布局系统（HorizontalLayoutGroup 等）的驱动器。
    // 每当 UI 尺寸发生变化时，它会找出该元素向上最近的 LayoutRoot（不受其他 LayoutGroup 控制的根节点），
    // 然后将这个 LayoutRoot 作为一个整体加入到 CanvasUpdateRegistry 的布局重建队列中。
    public class LayoutRebuilder : ICanvasElement
    {
        private RectTransform m_ToRebuild;
        //这里有几个原因我们需要缓存从 Transform 获取的 Hash 值：
        //  - 这是一个值类型 (struct)，.Net 会根据值类型的字段计算 Hash。
        //  - 字典 (Dictionary) 的键应该具有恒定的 Hash 值。
        //  - 从原生 (Native) 层来看，Transform 有可能变为空 (null)。
        // 我们在 IndexedSet 容器中使用这个结构，该容器的内部实现使用了字典。
        // 因此这个结构被用作字典的键，我们需要保证一个恒定的 Hash 值。
        private int m_CachedHashFromTransform;

        static ObjectPool<LayoutRebuilder> s_Rebuilders = new ObjectPool<LayoutRebuilder>(() => new LayoutRebuilder(), null, x => x.Clear());

        private void Initialize(RectTransform controller)
        {
            m_ToRebuild = controller;
            m_CachedHashFromTransform = controller.GetHashCode();
        }

        private void Clear()
        {
            m_ToRebuild = null;
            m_CachedHashFromTransform = 0;
        }

        static LayoutRebuilder()
        {
            RectTransform.reapplyDrivenProperties += ReapplyDrivenProperties;
        }

        static void ReapplyDrivenProperties(RectTransform driven)
        {
            MarkLayoutForRebuild(driven);
        }

        public Transform transform { get { return m_ToRebuild; }}

        /// <summary>
        /// 此 LayoutRebuilder 的原生表示是否已被销毁？
        /// </summary>
        public bool IsDestroyed()
        {
            return m_ToRebuild == null;
        }

        static void StripDisabledBehavioursFromList(List<Component> components)
        {
            components.RemoveAll(e => e is Behaviour && !((Behaviour)e).isActiveAndEnabled);
        }

        /// <summary>
        /// 强制立即重建布局元素及其子布局元素受计算影响的部分。
        /// </summary>
        /// <param name="layoutRoot">要执行布局重建的布局元素根节点。</param>
        /// <remarks>
        /// 布局系统的常规使用不应调用此方法。而应使用 MarkLayoutForRebuild，它会在下一个布局阶段 (layout pass) 触发延迟的布局重建。
        /// 延迟重建会自动以正确的顺序处理整个布局层级中的对象，并防止对同一布局元素进行多次重新计算。
        /// 但是，对于特殊的布局计算需求，可以使用 ::ref::ForceRebuildLayoutImmediate 立即解析子树的布局。
        /// 甚至可以从布局计算方法（如 ILayoutController.SetLayoutHorizontal 或 ILayoutController.SetLayoutVertical）内部执行此操作。
        /// 仅当尽管会带来额外的性能成本，但仍然无法避免多重布局阶段的情况下，才应使用此方法。
        /// </remarks>
        public static void ForceRebuildLayoutImmediate(RectTransform layoutRoot)
        {
            var rebuilder = s_Rebuilders.Get();
            rebuilder.Initialize(layoutRoot);
            rebuilder.Rebuild(CanvasUpdate.Layout);
            s_Rebuilders.Release(rebuilder);
        }

        public void Rebuild(CanvasUpdate executing)
        {
            switch (executing)
            {
                case CanvasUpdate.Layout:
                    // [重点注释 - 第二主线：自动布局系统]
                    // 自动布局是 UI 性能的“黑洞”。这里的执行顺序严格分为 4 步：
                    // 1. Calculate...Horizontal: 向上遍历，计算水平方向上的最小/首选尺寸
                    // 2. SetLayoutHorizontal:    向下遍历，设置水平方向上的实际尺寸和位置
                    // 3. Calculate...Vertical:   向上遍历，计算垂直方向上的最小/首选尺寸
                    // 4. SetLayoutVertical:      向下遍历，设置垂直方向上的实际尺寸和位置
                    // 每次修改层级内的一个属性，都会触发这四步的递归运算，所以尽量少用嵌套的 LayoutGroup。
                    //
                    // 令人遗憾的是，我们将对这棵树执行 2 次相同的 GetComponents 查询，
                    // 但是在进入下一个操作之前，每棵树都必须被完全迭代，
                    // 因此，如果要重用结果，就需要将结果存储在字典或类似结构中，
                    // 这可能比执行多次 GetComponents 带来的开销还要大。
                    PerformLayoutCalculation(m_ToRebuild, e => (e as ILayoutElement).CalculateLayoutInputHorizontal());
                    PerformLayoutControl(m_ToRebuild, e => (e as ILayoutController).SetLayoutHorizontal());
                    PerformLayoutCalculation(m_ToRebuild, e => (e as ILayoutElement).CalculateLayoutInputVertical());
                    PerformLayoutControl(m_ToRebuild, e => (e as ILayoutController).SetLayoutVertical());
                    break;
            }
        }

        private void PerformLayoutControl(RectTransform rect, UnityAction<Component> action)
        {
            if (rect == null)
                return;

            var components = ListPool<Component>.Get();
            rect.GetComponents(typeof(ILayoutController), components);
            StripDisabledBehavioursFromList(components);

            // 如果此 rect 上没有任何控制器，我们可以跳过这整个子树。
            // 我们也不需要考虑更深层子树上的控制器，因为它们会成为自己的根节点。
            if (components.Count > 0)
            {
                // 布局控制 (Layout control) 需要从上到下执行，父节点在其子节点之前完成，
                // 因为子节点依赖于父节点的尺寸。

                // 首先调用可能改变其自身 RectTransform 的布局控制器
                for (int i = 0; i < components.Count; i++)
                    if (components[i] is ILayoutSelfController)
                        action(components[i]);

                // 然后调用剩余的组件，例如那些在考虑自身 RectTransform 尺寸的同时改变其子节点的 Layout Group。
                for (int i = 0; i < components.Count; i++)
                    if (!(components[i] is ILayoutSelfController))
                    {
                        var scrollRect = components[i];

                        if (scrollRect && scrollRect is UnityEngine.UI.ScrollRect)
                        {
                            if (((UnityEngine.UI.ScrollRect)scrollRect).content != rect)
                                action(components[i]);
                        }
                        else
                        {
                            action(components[i]);
                        }
                    }

                for (int i = 0; i < rect.childCount; i++)
                    PerformLayoutControl(rect.GetChild(i) as RectTransform, action);
            }

            ListPool<Component>.Release(components);
        }

        private void PerformLayoutCalculation(RectTransform rect, UnityAction<Component> action)
        {
            if (rect == null)
                return;

            var components = ListPool<Component>.Get();
            rect.GetComponents(typeof(ILayoutElement), components);
            StripDisabledBehavioursFromList(components);

            // 如果此 rect 上没有任何控制器，我们可以跳过这整个子树。
            // 我们也不需要考虑更深层子树上的控制器，因为它们会成为自己的根节点。
            if (components.Count > 0  || rect.TryGetComponent(typeof(ILayoutGroup), out _))
            {
                // 布局计算 (Layout calculations) 需要自下而上执行，子节点在其父节点之前完成计算，
                // 因为父节点计算出的尺寸依赖于其子节点的尺寸。

                for (int i = 0; i < rect.childCount; i++)
                    PerformLayoutCalculation(rect.GetChild(i) as RectTransform, action);

                for (int i = 0; i < components.Count; i++)
                    action(components[i]);
            }

            ListPool<Component>.Release(components);
        }

        /// <summary>
        /// 标记指定的 RectTransform，使其在下一次布局阶段（Layout pass）被重新计算。
        /// </summary>
        /// <param name="rect">需要重建布局的 RectTransform。</param>
        // [重点注释 - 脏标记与根节点查找]
        // 核心性能点：当一个 UI 的大小发生变化时，它并不是只重排自己，
        // 而是通过 while 循环向上遍历寻找最顶层的激活的 ILayoutGroup（LayoutRoot）。
        // 然后把这个顶部的根节点加入到重建队列。
        // 这就是为什么深层次的 Layout 嵌套会导致一个小节点的变动引发整棵树的重新计算，性能消耗极大。
        public static void MarkLayoutForRebuild(RectTransform rect)
        {
            if (rect == null || rect.gameObject == null)
                return;

            var comps = ListPool<Component>.Get();
            bool validLayoutGroup = true;
            RectTransform layoutRoot = rect;
            var parent = layoutRoot.parent as RectTransform;
            while (validLayoutGroup && !(parent == null || parent.gameObject == null))
            {
                validLayoutGroup = false;
                parent.GetComponents(typeof(ILayoutGroup), comps);

                for (int i = 0; i < comps.Count; ++i)
                {
                    var cur = comps[i];
                    if (cur != null && cur is Behaviour && ((Behaviour)cur).isActiveAndEnabled)
                    {
                        validLayoutGroup = true;
                        layoutRoot = parent;
                        break;
                    }
                }

                parent = parent.parent as RectTransform;
            }

            // 我们知道，如果布局根节点与传入的 rect 不是同一个对象，那么它就是有效的（在上面已经检查过）。
            // 但如果它们是同一个对象，我们仍然需要检查它是否是一个有效的控制器。
            if (layoutRoot == rect && !ValidController(layoutRoot, comps))
            {
                ListPool<Component>.Release(comps);
                return;
            }

            MarkLayoutRootForRebuild(layoutRoot);
            ListPool<Component>.Release(comps);
        }

        private static bool ValidController(RectTransform layoutRoot, List<Component> comps)
        {
            if (layoutRoot == null || layoutRoot.gameObject == null)
                return false;

            layoutRoot.GetComponents(typeof(ILayoutController), comps);
            for (int i = 0; i < comps.Count; ++i)
            {
                var cur = comps[i];
                if (cur != null && cur is Behaviour && ((Behaviour)cur).isActiveAndEnabled)
                {
                    return true;
                }
            }

            return false;
        }

        private static void MarkLayoutRootForRebuild(RectTransform controller)
        {
            if (controller == null)
                return;

            var rebuilder = s_Rebuilders.Get();
            rebuilder.Initialize(controller);
            if (!CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(rebuilder))
                s_Rebuilders.Release(rebuilder);
        }

        public void LayoutComplete()
        {
            s_Rebuilders.Release(this);
        }

        public void GraphicUpdateComplete()
        {}

        public override int GetHashCode()
        {
            return m_CachedHashFromTransform;
        }

        /// <summary>
        /// 传入的重构建器 (rebuilder) 是否指向相同的 CanvasElement。
        /// </summary>
        /// <param name="obj">要比较的另一个对象</param>
        /// <returns>它们是否相等</returns>
        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        public override string ToString()
        {
            return "(Layout Rebuilder for) " + m_ToRebuild;
        }
    }
}
