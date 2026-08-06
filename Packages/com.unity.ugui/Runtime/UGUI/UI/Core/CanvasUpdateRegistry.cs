using System;
using System.Collections.Generic;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    /// <summary>
    /// Canvas 更新时调用的阶段枚举。
    /// </summary>
    /// <remarks> 如果修改此枚举，请同步修改 m_CanvasUpdateProfilerStrings 数组以保持匹配。</remarks>
    public enum CanvasUpdate
    {
        /// <summary>
        /// 布局前调用。
        /// </summary>
        Prelayout = 0,
        /// <summary>
        /// 布局时调用。
        /// </summary>
        Layout = 1,
        /// <summary>
        /// 布局后调用。
        /// </summary>
        PostLayout = 2,
        /// <summary>
        /// 渲染前调用。
        /// </summary>
        PreRender = 3,
        /// <summary>
        /// 渲染前晚期调用。
        /// </summary>
        LatePreRender = 4,
        /// <summary>
        /// 枚举的最大值，始终放在最后。
        /// </summary>
        MaxUpdateValue = 5
    }

    /// <summary>
    /// 可以存在于 Canvas 上的元素接口。
    /// </summary>
    public interface ICanvasElement
    {
        /// <summary>
        /// 在给定的阶段重建该元素。
        /// </summary>
        /// <param name="executing">当前正在重建的 CanvasUpdate 阶段。</param>
        void Rebuild(CanvasUpdate executing);

        /// <summary>
        /// 获取与此 ICanvasElement 关联的 Transform。
        /// </summary>
        Transform transform { get; }

        /// <summary>
        /// 当此 ICanvasElement 完成布局 (Layout) 时发送的回调。
        /// </summary>
        void LayoutComplete();

        /// <summary>
        /// 当此 ICanvasElement 完成图形重建 (Graphic rebuild) 时发送的回调。
        /// </summary>
        void GraphicUpdateComplete();

        /// <summary>
        /// 用于检查原生对象是否已被销毁。
        /// </summary>
        /// <returns>如果元素被视为已销毁，则返回 true。</returns>
        bool IsDestroyed();
    }

    /// <summary>
    /// 供 CanvasElements 注册自身以等待重建的注册中心。
    /// </summary>
    // [重点注释 - 渲染管线核心注册表]
    // UGUI 所有的脏标记（大小改变、颜色改变、文字改变等）最终都会汇总到这个单例类中。
    // 它维护了两个核心队列：布局重建队列 (m_LayoutRebuildQueue) 和 图形重建队列 (m_GraphicRebuildQueue)。
    // 并且通过监听 Canvas.willRenderCanvases 事件，在 Unity 每帧渲染前统一执行重建，这是 UI 能够批量处理状态变更的根本原因。
    public class CanvasUpdateRegistry
    {
        private static CanvasUpdateRegistry s_Instance;

        private bool m_PerformingLayoutUpdate;
        private bool m_PerformingGraphicUpdate;

        // 该数组与上方的 CanvasUpdate 枚举相匹配。请保持同步
        private string[] m_CanvasUpdateProfilerStrings = new string[] { "CanvasUpdate.Prelayout", "CanvasUpdate.Layout", "CanvasUpdate.PostLayout", "CanvasUpdate.PreRender", "CanvasUpdate.LatePreRender" };
        private const string m_CullingUpdateProfilerString = "ClipperRegistry.Cull";

        private readonly IndexedSet<ICanvasElement> m_LayoutRebuildQueue = new IndexedSet<ICanvasElement>();
        private readonly IndexedSet<ICanvasElement> m_GraphicRebuildQueue = new IndexedSet<ICanvasElement>();

        protected CanvasUpdateRegistry()
        {
            Canvas.willRenderCanvases += PerformUpdate;
        }

        /// <summary>
        /// 获取注册表的单例实例。
        /// </summary>
        public static CanvasUpdateRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new CanvasUpdateRegistry();
                return s_Instance;
            }
        }

        private bool ObjectValidForUpdate(ICanvasElement element)
        {
            var valid = element != null;

            var isUnityObject = element is Object;
            if (isUnityObject)
                valid = (element as Object) != null; // 这里利用了 UnityEngine.Object 重载的 == 运算符，检查原生对象是否依然存活。

            return valid;
        }

        private void CleanInvalidItems()
        {
            // MonoBehaviour 重载了用于判空的 == 运算符，它会检查底层对象是否已被销毁。
            // 如果直接处理具体的 MonoBehaviour 对象，这样判断是没有问题的；
            // 但在这个场景下，我们处理的是 ICanvasElement 接口的列表。
            // 对接口进行 == 判空并不会转发给 MonoBehaviour 的重载运算符，而只是检查接口引用是否为 null。
            // 因此，我们需要显式调用 IsDestroyed 来判断底层的原生对象是否已被销毁。

            var layoutRebuildQueueCount = m_LayoutRebuildQueue.Count;
            for (int i = layoutRebuildQueueCount - 1; i >= 0; --i)
            {
                var item = m_LayoutRebuildQueue[i];
                if (item == null)
                {
                    m_LayoutRebuildQueue.RemoveAt(i);
                    continue;
                }

                if (item.IsDestroyed())
                {
                    m_LayoutRebuildQueue.RemoveAt(i);
                    item.LayoutComplete();
                }
            }

            var graphicRebuildQueueCount = m_GraphicRebuildQueue.Count;
            for (int i = graphicRebuildQueueCount - 1; i >= 0; --i)
            {
                var item = m_GraphicRebuildQueue[i];
                if (item == null)
                {
                    m_GraphicRebuildQueue.RemoveAt(i);
                    continue;
                }

                if (item.IsDestroyed())
                {
                    m_GraphicRebuildQueue.RemoveAt(i);
                    item.GraphicUpdateComplete();
                }
            }
        }

        private static readonly Comparison<ICanvasElement> s_SortLayoutFunction = SortLayoutList;
        private void PerformUpdate()
        {
            // [重点注释 - 第一主线：渲染与网格重建管线]
            // 这里是 UGUI 渲染的“心脏”，响应 Canvas.willRenderCanvases 事件。
            // 每次渲染前，它会将需要更新的 UI 分为 Layout（布局）和 Graphic（图形）两个阶段来处理。
            UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);
            CleanInvalidItems();

            m_PerformingLayoutUpdate = true;

            // 1. 布局重建 (Layout Rebuild)
            // 根据层级深度排序，确保父节点先于子节点布局（向上遍历排版要求）。
            m_LayoutRebuildQueue.Sort(s_SortLayoutFunction);

            for (int i = 0; i <= (int)CanvasUpdate.PostLayout; i++)
            {
                UnityEngine.Profiling.Profiler.BeginSample(m_CanvasUpdateProfilerStrings[i]);

                for (int j = 0; j < m_LayoutRebuildQueue.Count; j++)
                {
                    var rebuild = m_LayoutRebuildQueue[j];
                    try
                    {
                        if (ObjectValidForUpdate(rebuild))
                            rebuild.Rebuild((CanvasUpdate)i);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, rebuild.transform);
                    }
                }
                UnityEngine.Profiling.Profiler.EndSample();
            }

            for (int i = 0; i < m_LayoutRebuildQueue.Count; ++i)
                m_LayoutRebuildQueue[i].LayoutComplete();

            m_LayoutRebuildQueue.Clear();
            m_PerformingLayoutUpdate = false;
            UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);
            UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Render);

            // 现在布局已经完成，执行裁剪 (culling)...
            // [重点注释] 执行裁剪，例如 RectMask2D 的计算
            UnityEngine.Profiling.Profiler.BeginSample(m_CullingUpdateProfilerString);
            ClipperRegistry.instance.Cull();
            UnityEngine.Profiling.Profiler.EndSample();

            m_PerformingGraphicUpdate = true;

            // 2. 图形重建 (Graphic Rebuild)
            // 遍历所有被标记为 Dirty（Vertices 或 Material）的图形元素，触发 Rebuild 以重新生成网格或更新材质。
            for (var i = (int)CanvasUpdate.PreRender; i < (int)CanvasUpdate.MaxUpdateValue; i++)
            {
                UnityEngine.Profiling.Profiler.BeginSample(m_CanvasUpdateProfilerStrings[i]);
                for (var k = 0; k < m_GraphicRebuildQueue.Count; k++)
                {
                    try
                    {
                        var element = m_GraphicRebuildQueue[k];
                        if (ObjectValidForUpdate(element))
                            element.Rebuild((CanvasUpdate)i);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, m_GraphicRebuildQueue[k].transform);
                    }
                }
                UnityEngine.Profiling.Profiler.EndSample();
            }

            for (int i = 0; i < m_GraphicRebuildQueue.Count; ++i)
                m_GraphicRebuildQueue[i].GraphicUpdateComplete();

            m_GraphicRebuildQueue.Clear();
            m_PerformingGraphicUpdate = false;
            UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Render);
        }

        private static int ParentCount(Transform child)
        {
            if (child == null)
                return 0;

            var parent = child.parent;
            int count = 0;
            while (parent != null)
            {
                count++;
                parent = parent.parent;
            }
            return count;
        }

        // [重点注释 - 布局排序依据]
        // 布局重建为什么需要排序？
        // 因为 UI 的排版通常是父节点决定子节点的可用空间。通过判断 Transform 的层级深度（ParentCount），
        // 确保层级浅的（父节点）排在前面先被重建，层级深的（子节点）后被重建。
        // 如果顺序反了，子节点计算完大小后，父节点尺寸又变了，就会导致排版错误。
        private static int SortLayoutList(ICanvasElement x, ICanvasElement y)
        {
            Transform t1 = x.transform;
            Transform t2 = y.transform;

            return ParentCount(t1) - ParentCount(t2);
        }

        /// <summary>
        /// 尝试将给定元素添加到布局重建列表。
        /// 如果成功添加，将不会有返回值。
        /// </summary>
        /// <param name="element">需要重建的元素。</param>
        // [重点注释 - 注册布局重建]
        // 当 UI 的 RectTransform 发生变化（如宽高、锚点改变），或者包含 LayoutGroup 的排版改变时，
        // 会调用此方法将其加入布局重建队列。注意，UGUI 使用了 IndexedSet 来去重，
        // 这意味着同一帧内你对同一个 UI 的尺寸修改 N 次，最终也只会在队列里保留 1 个实例，只重建 1 次。
        public static void RegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        /// <summary>
        /// 尝试将给定元素添加到布局重建列表。
        /// </summary>
        /// <param name="element">需要重建的元素。</param>
        /// <returns>
        /// 如果元素成功添加到重建列表，则返回 True。
        /// 如果当前已经在图形更新循环 (Graphic Update loop) 中，或者该元素已经被添加到列表中，则返回 False。
        /// </returns>
        public static bool TryRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        private bool InternalRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_LayoutRebuildQueue.Contains(element))
                return false;

            /* TODO: this likely should be here but causes the error to show just resizing the game view (case 739376)
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format("Trying to add {0} for layout rebuild while we are already inside a layout rebuild loop. This is not supported.", element));
                return false;
            }*/

            return m_LayoutRebuildQueue.AddUnique(element);
        }

        /// <summary>
        /// 尝试将给定元素添加到重建列表。
        /// 如果成功添加，将不会有返回值。
        /// </summary>
        /// <param name="element">需要重建的元素。</param>
        // [重点注释 - 注册图形重建]
        // 当 UI 的颜色、材质、贴图或 Text 文字内容发生变化时（如 Image 的 sprite 被替换），
        // 会调用此方法将其加入图形重建队列。同样会利用 IndexedSet 自动去重。
        // 这一步骤的处理在 PerformUpdate 中晚于布局队列。
        public static void RegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        /// <summary>
        /// 尝试将给定元素添加到重建列表。
        /// </summary>
        /// <param name="element">需要重建的元素。</param>
        /// <returns>
        /// 如果元素成功添加到重建列表，则返回 True。
        /// 如果当前已经在图形更新循环 (Graphic Update loop) 中，或者该元素已经被添加到列表中，则返回 False。
        /// </returns>
        public static bool TryRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        private bool InternalRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format("Trying to add {0} for graphic rebuild while we are already inside a graphic rebuild loop. This is not supported.", element));
                return false;
            }

            return m_GraphicRebuildQueue.AddUnique(element);
        }

        /// <summary>
        /// 将给定元素从图形和布局重建列表中移除。
        /// </summary>
        /// <param name="element"></param>
        public static void UnRegisterCanvasElementForRebuild(ICanvasElement element)
        {
            instance.InternalUnRegisterCanvasElementForLayoutRebuild(element);
            instance.InternalUnRegisterCanvasElementForGraphicRebuild(element);
        }

        /// <summary>
        /// 将给定元素从图形和布局重建列表中禁用。
        /// </summary>
        /// <param name="element"></param>
        public static void DisableCanvasElementForRebuild(ICanvasElement element)
        {
            instance.InternalDisableCanvasElementForLayoutRebuild(element);
            instance.InternalDisableCanvasElementForGraphicRebuild(element);
        }

        private void InternalUnRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
                return;
            }

            element.LayoutComplete();
            instance.m_LayoutRebuildQueue.Remove(element);
        }

        private void InternalUnRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
                return;
            }
            element.GraphicUpdateComplete();
            instance.m_GraphicRebuildQueue.Remove(element);
        }

        private void InternalDisableCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
                return;
            }

            element.LayoutComplete();
            instance.m_LayoutRebuildQueue.DisableItem(element);
        }

        private void InternalDisableCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
                return;
            }
            element.GraphicUpdateComplete();
            instance.m_GraphicRebuildQueue.DisableItem(element);
        }

        /// <summary>
        /// 当前是否正在计算图形布局。
        /// </summary>
        /// <returns>如果重建循环正处于 CanvasUpdate.Prelayout、CanvasUpdate.Layout 或 CanvasUpdate.Postlayout，则返回 True。</returns>
        public static bool IsRebuildingLayout()
        {
            return instance.m_PerformingLayoutUpdate;
        }

        /// <summary>
        /// 当前是否正在进行图形重建。
        /// </summary>
        /// <returns>如果重建循环正处于 CanvasUpdate.PreRender 或 CanvasUpdate.Render，则返回 True。</returns>
        public static bool IsRebuildingGraphics()
        {
            return instance.m_PerformingGraphicUpdate;
        }
    }
}
