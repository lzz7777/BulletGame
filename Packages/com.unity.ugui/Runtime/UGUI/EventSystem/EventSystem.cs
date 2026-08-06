using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UnityEngine.EventSystems
{
    [AddComponentMenu("Event/Event System")]
    [DisallowMultipleComponent]
    /// <summary>
    /// Handles input, raycasting, and sending events.
    /// </summary>
    /// <remarks>
    /// EventSystem 负责处理和管理 Unity 场景中的事件。一个场景通常只应包含一个 EventSystem。
    /// EventSystem 与许多模块配合工作，它主要保存状态，并将功能委托给特定的、可重写的组件（InputModule）。
    /// 启动时，EventSystem 会搜索挂载在同一 GameObject 上的所有 BaseInputModule 并添加到内部列表。
    /// 在每帧 Update 时，它会调用所有附加模块的 UpdateModules，模块可以在此修改内部状态。
    /// 所有模块更新后，它会调用当前激活模块的 Process 方法，在此进行自定义的输入处理和事件分发。
    /// </remarks>
    // [重点注释 - 事件大管家]
    // 它是整个 UGUI 甚至 3D 物体点击事件的驱动核心。它本身不处理鼠标或触摸输入，
    // 而是像一个轮询引擎，在每帧的 Update 里，调用当前激活的 InputModule（如 StandaloneInputModule）
    // 的 Process 方法来处理真正的输入逻辑。
    public class EventSystem : UIBehaviour
    {
        private List<BaseInputModule> m_SystemInputModules = new List<BaseInputModule>();

        private BaseInputModule m_CurrentInputModule;

        private  static List<EventSystem> m_EventSystems = new List<EventSystem>();

        /// <summary>
        /// 返回当前的 EventSystem。
        /// </summary>
        public static EventSystem current
        {
            get { return m_EventSystems.Count > 0 ? m_EventSystems[0] : null; }
            set
            {
                int index = m_EventSystems.IndexOf(value);

                if (index > 0)
                {
                    m_EventSystems.RemoveAt(index);
                    m_EventSystems.Insert(0, value);
                }
                else if (index < 0)
                {
                    Debug.LogError("Failed setting EventSystem.current to unknown EventSystem " + value);
                }
            }
        }

        [SerializeField]
        [FormerlySerializedAs("m_Selected")]
        private GameObject m_FirstSelected;

        [SerializeField]
        private bool m_sendNavigationEvents = true;

        /// <summary>
        /// EventSystem 是否应允许导航事件 (移动 / 提交 / 取消)。
        /// </summary>
        public bool sendNavigationEvents
        {
            get { return m_sendNavigationEvents; }
            set { m_sendNavigationEvents = value; }
        }

        [SerializeField]
        private int m_DragThreshold = 10;

        /// <summary>
        /// 拖拽判定的像素软区域阈值。
        /// </summary>
        public int pixelDragThreshold
        {
            get { return m_DragThreshold; }
            set { m_DragThreshold = value; }
        }

        private GameObject m_CurrentSelected;

        /// <summary>
        /// 当前激活的 EventSystems.BaseInputModule。
        /// </summary>
        public BaseInputModule currentInputModule
        {
            get { return m_CurrentInputModule; }
        }

        /// <summary>
        /// 同一时间只能有一个对象被选中。例如：控制器选择的按钮。
        /// </summary>
        public GameObject firstSelectedGameObject
        {
            get { return m_FirstSelected; }
            set { m_FirstSelected = value; }
        }

        /// <summary>
        /// 当前被 EventSystem 视为激活的 GameObject。
        /// </summary>
        public GameObject currentSelectedGameObject
        {
            get { return m_CurrentSelected; }
        }

        [Obsolete("lastSelectedGameObject is no longer supported")]
        public GameObject lastSelectedGameObject
        {
            get { return null; }
        }

        private bool m_HasFocus = true;

        /// <summary>
        /// 标志位：表明 EventSystem 是否认为它应该基于焦点状态暂停工作。
        /// </summary>
        /// <remarks>
        /// 在各个 InputModule 内部使用，以决定在应用程序失去焦点时，模块是否还应该继续 Tick（滴答更新）。
        /// </remarks>
        public bool isFocused
        {
            get { return m_HasFocus; }
        }

        protected EventSystem()
        {}

        /// <summary>
        /// 重新计算内部的 BaseInputModule 列表。
        /// </summary>
        public void UpdateModules()
        {
            GetComponents(m_SystemInputModules);
            var systemInputModulesCount = m_SystemInputModules.Count;
            for (int i = systemInputModulesCount - 1; i >= 0; i--)
            {
                if (m_SystemInputModules[i] && m_SystemInputModules[i].IsActive())
                    continue;

                m_SystemInputModules.RemoveAt(i);
            }
        }

        private bool m_SelectionGuard;

        /// <summary>
        /// 如果 EventSystem 当前正在执行 SetSelectedGameObject 逻辑，则返回 true。
        /// </summary>
        public bool alreadySelecting
        {
            get { return m_SelectionGuard; }
        }

        /// <summary>
        /// 将对象设置为被选中状态。将会向旧的被选中对象发送 OnDeselect 事件，向新的被选中对象发送 OnSelect 事件。
        /// </summary>
        /// <param name="selected">要选中的 GameObject。</param>
        /// <param name="pointer">关联的事件数据 (EventData)。</param>
        // [重点注释 - UI焦点切换]
        // 核心方法：负责切换当前的焦点 UI 对象。它利用 ExecuteEvents 
        // 自动分发脱焦 (Deselect) 和获焦 (Select) 事件给所有挂载了对应接口的脚本。
        public void SetSelectedGameObject(GameObject selected, BaseEventData pointer)
        {
            if (m_SelectionGuard)
            {
                Debug.LogError("Attempting to select " + selected +  "while already selecting an object.");
                return;
            }

            m_SelectionGuard = true;
            if (selected == m_CurrentSelected)
            {
                m_SelectionGuard = false;
                return;
            }

            // Debug.Log("Selection: new (" + selected + ") old (" + m_CurrentSelected + ")");
            ExecuteEvents.Execute(m_CurrentSelected, pointer, ExecuteEvents.deselectHandler);
            m_CurrentSelected = selected;
            ExecuteEvents.Execute(m_CurrentSelected, pointer, ExecuteEvents.selectHandler);
            m_SelectionGuard = false;
        }

        private BaseEventData m_DummyData;
        private BaseEventData baseEventDataCache
        {
            get
            {
                if (m_DummyData == null)
                    m_DummyData = new BaseEventData(this);

                return m_DummyData;
            }
        }

        /// <summary>
        /// 将对象设置为被选中状态。将会向旧的被选中对象发送 OnDeselect 事件，向新的被选中对象发送 OnSelect 事件。
        /// </summary>
        /// <param name="selected">要选中的 GameObject。</param>
        public void SetSelectedGameObject(GameObject selected)
        {
            SetSelectedGameObject(selected, baseEventDataCache);
        }

        private static int RaycastComparer(RaycastResult lhs, RaycastResult rhs)
        {
            if (lhs.module != rhs.module)
            {
                var lhsEventCamera = lhs.module.eventCamera;
                var rhsEventCamera = rhs.module.eventCamera;
                if (lhsEventCamera != null && rhsEventCamera != null && lhsEventCamera.depth != rhsEventCamera.depth)
                {
                    // need to reverse the standard compareTo
                    if (lhsEventCamera.depth < rhsEventCamera.depth)
                        return 1;
                    if (lhsEventCamera.depth == rhsEventCamera.depth)
                        return 0;

                    return -1;
                }

                if (lhs.module.sortOrderPriority != rhs.module.sortOrderPriority)
                    return rhs.module.sortOrderPriority.CompareTo(lhs.module.sortOrderPriority);

                if (lhs.module.renderOrderPriority != rhs.module.renderOrderPriority)
                    return rhs.module.renderOrderPriority.CompareTo(lhs.module.renderOrderPriority);
            }

            // Renderer sorting
            if (lhs.sortingLayer != rhs.sortingLayer)
            {
                // Uses the layer value to properly compare the relative order of the layers.
                var rid = SortingLayer.GetLayerValueFromID(rhs.sortingLayer);
                var lid = SortingLayer.GetLayerValueFromID(lhs.sortingLayer);
                return rid.CompareTo(lid);
            }

            if (lhs.sortingOrder != rhs.sortingOrder)
                return rhs.sortingOrder.CompareTo(lhs.sortingOrder);

            // comparing depth only makes sense if the two raycast results have the same root canvas (case 912396)
            if (lhs.depth != rhs.depth && lhs.module.rootRaycaster == rhs.module.rootRaycaster)
                return rhs.depth.CompareTo(lhs.depth);

            if (lhs.distance != rhs.distance)
                return lhs.distance.CompareTo(rhs.distance);

            #if PACKAGE_PHYSICS2D
			// Sorting group
            if (lhs.sortingGroupID != SortingGroup.invalidSortingGroupID && rhs.sortingGroupID != SortingGroup.invalidSortingGroupID)
            {
                if (lhs.sortingGroupID != rhs.sortingGroupID)
                    return lhs.sortingGroupID.CompareTo(rhs.sortingGroupID);
                if (lhs.sortingGroupOrder != rhs.sortingGroupOrder)
                    return rhs.sortingGroupOrder.CompareTo(lhs.sortingGroupOrder);
            }
            #endif

            return lhs.index.CompareTo(rhs.index);
        }

        private static readonly Comparison<RaycastResult> s_RaycastComparer = RaycastComparer;

        /// <summary>
        /// 使用所有已配置的 BaseRaycaster 在场景中投射射线进行检测。
        /// </summary>
        /// <param name="eventData">当前的指针数据 (PointerEventData)。</param>
        /// <param name="raycastResults">用于填充 '击中' 结果的列表。</param>
        public void RaycastAll(PointerEventData eventData, List<RaycastResult> raycastResults)
        {
            raycastResults.Clear();
            var modules = RaycasterManager.GetRaycasters();
            var modulesCount = modules.Count;
            for (int i = 0; i < modulesCount; ++i)
            {
                var module = modules[i];
                if (module == null || !module.IsActive())
                    continue;

                module.Raycast(eventData, raycastResults);
            }

            raycastResults.Sort(s_RaycastComparer);
        }

        /// <summary>
        /// 带有给定 ID 的指针 (如鼠标或触摸) 是否悬停在 EventSystem 对象（UI）上方？
        /// </summary>
        public bool IsPointerOverGameObject()
        {
            return IsPointerOverGameObject(PointerInputModule.kMouseLeftId);
        }

        /// <summary>
        /// 带有给定 ID 的指针 (如鼠标或触摸) 是否悬停在 EventSystem 对象（UI）上方？
        /// </summary>
        /// <remarks>
        /// 如果你在没有参数的情况下使用 IsPointerOverGameObject()，它指向的是“鼠标左键”(pointerId = -1)；
        /// 因此，当你在触摸 (Touch) 时使用 IsPointerOverGameObject，你应该考虑向其传递一个具体的 pointerId（即手指 ID）。
        /// 请注意，对于触摸，IsPointerOverGameObject 应该与 ''OnMouseDown()''、''Input.GetMouseButtonDown(0)'' 
        /// 或者 ''Input.GetTouch(0).phase == TouchPhase.Began'' 结合使用。
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.EventSystems;
        ///
        /// public class MouseExample : MonoBehaviour
        /// {
        ///     void Update()
        ///     {
        ///         // 检查鼠标左键是否被点击
        ///         if (Input.GetMouseButtonDown(0))
        ///         {
        ///             // 检查鼠标是否点击在 UI 元素上
        ///             if (EventSystem.current.IsPointerOverGameObject())
        ///             {
        ///                 Debug.Log("Clicked on the UI");
        ///             }
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        // [重点注释 - 点击穿透检测]
        // 极其常用的 API，用于判断当前点击（或触摸）是否点在了 UI 上，以此来屏蔽角色移动或 3D 物体的点击。
        // 原理是询问当前 InputModule：这个 pointerId 下是否已经有碰到的 UI 元素。
        public bool IsPointerOverGameObject(int pointerId)
        {
            return m_CurrentInputModule != null && m_CurrentInputModule.IsPointerOverGameObject(pointerId);
        }

        // This code is disabled unless the UI Toolkit package or the com.unity.modules.uielements module are present.
        // The UIElements module is always present in the Editor but it can be stripped from a project build if unused.
#if PACKAGE_UITOOLKIT
        private struct UIToolkitOverrideConfig
        {
            public EventSystem activeEventSystem;
            public bool sendEvents;
            public bool createPanelGameObjectsOnStart;
        }

        private static UIToolkitOverrideConfig s_UIToolkitOverride = new UIToolkitOverrideConfig
        {
            activeEventSystem = null,
            sendEvents = true,
            createPanelGameObjectsOnStart = true
        };

        private bool isUIToolkitActiveEventSystem =>
            s_UIToolkitOverride.activeEventSystem == this || s_UIToolkitOverride.activeEventSystem == null;

        private bool sendUIToolkitEvents =>
            s_UIToolkitOverride.sendEvents && isUIToolkitActiveEventSystem;

        private bool createUIToolkitPanelGameObjectsOnStart =>
            s_UIToolkitOverride.createPanelGameObjectsOnStart && isUIToolkitActiveEventSystem;
#endif

        /// <summary>
        /// Sets how UI Toolkit runtime panels receive events and handle selection
        /// when interacting with other objects that use the EventSystem, such as components from the Unity UI package.
        /// </summary>
        /// <param name="activeEventSystem">
        /// The EventSystem used to override UI Toolkit panel events and selection.
        /// If activeEventSystem is null, UI Toolkit panels will use current enabled EventSystem
        /// or, if there is none, the default InputManager-based event system will be used.
        /// </param>
        /// <param name="sendEvents">
        /// If true, UI Toolkit events will come from this EventSystem
        /// instead of the default InputManager-based event system.
        /// </param>
        /// <param name="createPanelGameObjectsOnStart">
        /// If true, UI Toolkit panels' unassigned selectableGameObject will be automatically initialized
        /// with children GameObjects of this EventSystem on Start.
        /// </param>
        public static void SetUITookitEventSystemOverride(EventSystem activeEventSystem, bool sendEvents = true, bool createPanelGameObjectsOnStart = true)
        {
#if PACKAGE_UITOOLKIT
            UIElementsRuntimeUtility.UnregisterEventSystem(UIElementsRuntimeUtility.activeEventSystem);

            s_UIToolkitOverride = new UIToolkitOverrideConfig
            {
                activeEventSystem = activeEventSystem,
                sendEvents = sendEvents,
                createPanelGameObjectsOnStart = createPanelGameObjectsOnStart,
            };

            if (sendEvents)
            {
                var eventSystem = activeEventSystem != null ? activeEventSystem : EventSystem.current;
                if (eventSystem.isActiveAndEnabled)
                    UIElementsRuntimeUtility.RegisterEventSystem(activeEventSystem);
            }
#endif
        }

#if PACKAGE_UITOOLKIT
        private bool m_Started;
        private bool m_IsTrackingUIToolkitPanels;

        private void StartTrackingUIToolkitPanels()
        {
            if (createUIToolkitPanelGameObjectsOnStart)
            {
                foreach (BaseRuntimePanel panel in UIElementsRuntimeUtility.GetSortedPlayerPanels())
                {
                    CreateUIToolkitPanelGameObject(panel);
                }
                UIElementsRuntimeUtility.onCreatePanel += CreateUIToolkitPanelGameObject;
                m_IsTrackingUIToolkitPanels = true;
            }
        }

        private void StopTrackingUIToolkitPanels()
        {
            if (m_IsTrackingUIToolkitPanels)
            {
                UIElementsRuntimeUtility.onCreatePanel -= CreateUIToolkitPanelGameObject;
                m_IsTrackingUIToolkitPanels = false;
            }
        }

        private void CreateUIToolkitPanelGameObject(BaseRuntimePanel panel)
        {
            if (panel.selectableGameObject == null)
            {
                var go = new GameObject(panel.name, typeof(PanelEventHandler), typeof(PanelRaycaster));
                go.transform.SetParent(transform);
                panel.selectableGameObject = go;
                panel.destroyed += () => DestroyImmediate(go);
            }
        }
#endif

        protected override void Start()
        {
            base.Start();

#if PACKAGE_UITOOLKIT
            m_Started = true;
            StartTrackingUIToolkitPanels();
#endif
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_EventSystems.Add(this);

#if PACKAGE_UITOOLKIT
            if (m_Started && !m_IsTrackingUIToolkitPanels)
            {
                StartTrackingUIToolkitPanels();
            }
            if (sendUIToolkitEvents)
            {
                UIElementsRuntimeUtility.RegisterEventSystem(this);
            }
#endif
        }

        protected override void OnDisable()
        {
#if PACKAGE_UITOOLKIT
            StopTrackingUIToolkitPanels();
            UIElementsRuntimeUtility.UnregisterEventSystem(this);
#endif

            if (m_CurrentInputModule != null)
            {
                m_CurrentInputModule.DeactivateModule();
                m_CurrentInputModule = null;
            }

            m_EventSystems.Remove(this);

            base.OnDisable();
        }

        private void TickModules()
        {
            var systemInputModulesCount = m_SystemInputModules.Count;
            for (var i = 0; i < systemInputModulesCount; i++)
            {
                if (m_SystemInputModules[i] != null)
                    m_SystemInputModules[i].UpdateModule();
            }
        }

        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            m_HasFocus = hasFocus;
            if (!m_HasFocus)
                TickModules();
        }

        // [重点注释 - 核心事件循环]
        // 1. TickModules: 遍历所有模块的 UpdateModule
        // 2. 找到第一个 ShouldActivateModule 的模块并激活它（ChangeEventModule）
        // 3. 调用当前激活模块的 Process() 执行真正的输入和射线检测逻辑
        protected virtual void Update()
        {
            if (current != this)
                return;
            TickModules();

            bool changedModule = false;
            var systemInputModulesCount = m_SystemInputModules.Count;
            for (var i = 0; i < systemInputModulesCount; i++)
            {
                var module = m_SystemInputModules[i];
                if (module.IsModuleSupported() && module.ShouldActivateModule())
                {
                    if (m_CurrentInputModule != module)
                    {
                        ChangeEventModule(module);
                        changedModule = true;
                    }
                    break;
                }
            }

            // no event module set... set the first valid one...
            if (m_CurrentInputModule == null)
            {
                for (var i = 0; i < systemInputModulesCount; i++)
                {
                    var module = m_SystemInputModules[i];
                    if (module.IsModuleSupported())
                    {
                        ChangeEventModule(module);
                        changedModule = true;
                        break;
                    }
                }
            }

            if (!changedModule && m_CurrentInputModule != null)
                m_CurrentInputModule.Process();

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                int eventSystemCount = 0;
                for (int i = 0; i < m_EventSystems.Count; i++)
                {
                    if (m_EventSystems[i].GetType() == typeof(EventSystem))
                        eventSystemCount++;
                }

                if (eventSystemCount > 1)
                    Debug.LogWarning("There are " + eventSystemCount + " event systems in the scene. Please ensure there is always exactly one event system in the scene");
            }
#endif
        }

        private void ChangeEventModule(BaseInputModule module)
        {
            if (m_CurrentInputModule == module)
                return;

            if (m_CurrentInputModule != null)
                m_CurrentInputModule.DeactivateModule();

            if (module != null)
                module.ActivateModule();
            m_CurrentInputModule = module;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>Selected:</b>" + currentSelectedGameObject);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(m_CurrentInputModule != null ? m_CurrentInputModule.ToString() : "No module");
            return sb.ToString();
        }
    }
}
