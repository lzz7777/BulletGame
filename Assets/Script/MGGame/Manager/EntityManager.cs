using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace XN
{
    public class EntityManager : MonoSingleton<EntityManager>
    {
        // ========================== 性能优化：无反射强类型调用包装器 ==========================
        private interface IUpdateActionCaller
        {
            void Call(IComponent comp, float dt);
        }

        private class UpdateActionCaller<T> : IUpdateActionCaller where T : IComponent
        {
            private readonly Action<T, float> _action;

            public UpdateActionCaller(MethodInfo methodInfo)
            {
                // 利用 Delegate.CreateDelegate 将 MethodInfo 转为强类型委托，此操作只在初始化执行一次
                _action = (Action<T, float>)Delegate.CreateDelegate(typeof(Action<T, float>), methodInfo);
            }

            public void Call(IComponent comp, float dt)
            {
                // 无装箱，无拆箱，无反射，直接强转执行委托！
                _action((T)comp, dt);
            }
        }
        // ====================================================================================

        private Dictionary<long, Entity> _entitiesDic { set; get; } = new();
        private Dictionary<Type, List<IComponent>> _componentCache { set; get; } = new();
        private Dictionary<EntityType, List<long>> _entityTypeDic { set; get; } = new();
        
        private Dictionary<Type, Stack<object>> _objectPool { set; get; } = new();

        // 存所有需要 Update 的委托：Key 是组件类型，Value 是对应的静态扩展方法委托
        private Dictionary<Type, Action<IComponent, float>> _updateDelegates = new();
        
#if UNITY_EDITOR
        public Transform EntityRoot => _entityRoot;
        private Transform _entityRoot;
        private Dictionary<long, EntityViewer> _viewers = new();
#endif

        protected override void OnInit()
        {
            // 自动收集所有 Update 扩展方法！
            CollectUpdateSystems();
            
#if UNITY_EDITOR
            _entityRoot = new GameObject("EntityRoot").transform;
            _viewers = new Dictionary<long, EntityViewer>();
            DontDestroyOnLoad(_entityRoot.gameObject);

            EventsManager.AddListener<long>(GameEnum.UpdateEntityViewerEvent, OnUpdateEntityViewer);
#endif
        }

        protected override void OnRemove()
        {
#if UNITY_EDITOR
            if (_entityRoot != null)
            {
                Destroy(_entityRoot.gameObject);
            }

            EventsManager.RemoveListener<long>(GameEnum.UpdateEntityViewerEvent, OnUpdateEntityViewer);
#endif
        }

        private void CollectUpdateSystems()
        {
            // 找到所有打了 [UpdateSystem] 的静态方法
            var methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested) // 找静态类
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.GetCustomAttribute<UpdateSystemAttribute>() != null);

            foreach (var method in methods)
            {
                // 扩展方法的第一个参数就是 Component 的类型 (this CarInfoComponent self)
                var parameters = method.GetParameters();
                if (parameters.Length != 2 || !typeof(IComponent).IsAssignableFrom(parameters[0].ParameterType))
                    continue;

                Type componentType = parameters[0].ParameterType;

                // 由于我们事先不知道确切的子类类型，但需要一个统一的入口
                // 真正的高性能无GC方案：利用强类型反射缓存委托
                var callerType = typeof(UpdateActionCaller<>).MakeGenericType(componentType);
                var caller = (IUpdateActionCaller)Activator.CreateInstance(callerType, method);
                
                _updateDelegates[componentType] = caller.Call;
            }
        }

        private void Update()
        {
            float dt =  Time.deltaTime;
            // 核心：自动遍历并调用扩展方法！
            foreach (var kvp in _updateDelegates)
            {
                Type compType = kvp.Key;
                Action<IComponent, float> updateAction = kvp.Value;

                var components = GetComponents(compType);
                if (components == null) continue;

                // 倒序遍历防止删除报错
                for (int i = components.Count - 1; i >= 0; i--)
                {
                    var comp = components[i];
                    if (comp == null || comp.Entity == null || comp.Entity.IsDispose) continue;

                    updateAction(comp, dt);
                }
            }
        }

        public void RegisterComponent(IComponent comp)
        {
            Type type = comp.GetType();
            if (!_componentCache.TryGetValue(type, out var list))
            {
                list = new List<IComponent>();
                _componentCache[type] = list;
            }

            comp.TypeIndex = list.Count;
            list.Add(comp);
        }

        public void UnregisterComponent(IComponent comp)
        {
            Type type = comp.GetType();
            if (_componentCache.TryGetValue(type, out var list))
            {
                if (comp.TypeIndex >= 0 && comp.TypeIndex < list.Count)
                {
                    int index = comp.TypeIndex;
                    int lastIndex = list.Count - 1;

                    if (index != lastIndex)
                    {
                        var lastComp = list[lastIndex];
                        list[index] = lastComp;
                        lastComp.TypeIndex = index;
                    }

                    list.RemoveAt(lastIndex);
                }
                else
                {
                    list.Remove(comp);
                }

                comp.TypeIndex = -1;
            }
        }

        public List<IComponent> GetComponents(Type type)
        {
            return _componentCache.TryGetValue(type, out var list) ? list : null;
        }

        public List<IComponent> GetComponents<T>() where T : IComponent
        {
            return GetComponents(typeof(T));
        }

        public T GetFromPool<T>() where T : new()
        {
            _objectPool.TryAdd(typeof(T), new());
            var poolData = _objectPool[typeof(T)];

            if (poolData.Count > 0)
            {
                return (T)poolData.Pop();
            }

            return new T();
        }

        public void ReturnToPool<T>(T t)
        {
            _objectPool.TryAdd(typeof(T), new());
            var poolData = _objectPool[typeof(T)];

            poolData.Push(t);
        }
        
        public Entity CreateEntity(EntityType entityTag, bool isFromPool = true)
        {
            Entity entity;

            if (isFromPool)
            {
                entity = GetFromPool<Entity>();
            }
            else
            {
                entity = new Entity();
            }

            entity.Init(entityTag, isFromPool);
            _entityTypeDic.TryAdd(entityTag, new());
            _entityTypeDic[entityTag].Add(entity.Id);
            _entitiesDic[entity.Id] = entity;

#if UNITY_EDITOR
            var go = new GameObject($"Entity_{entity.Tag}_{entity.Id}");
            var viewer = go.AddComponent<EntityViewer>();
            viewer.Entity = entity;
            viewer.transform.SetParent(_entityRoot);
            _viewers[entity.Id] = viewer;
#endif

            return entity;
        }

        public void RemoveEntity(long id) => RemoveEntity(GetEntityById(id));

        public void RemoveEntity(Entity entity)
        {
#if UNITY_EDITOR
            if (_viewers.TryGetValue(entity.Id, out var viewer))
            {
                if (viewer != null) Destroy(viewer.gameObject);
                _viewers.Remove(entity.Id);
            }
#endif

            var childs = entity.GetChildren();
            for (int i = childs.Count - 1; i >= 0; i--)
            {
                RemoveEntity(childs[i]);
            }

            entity.OnDestroy();

            var parent = entity.GetParent();
            if (parent != null)
            {
                parent.RemoveChild(entity);
            }

            if (entity.IsFromPool)
            {
                ReturnToPool(entity);
            }

            _entityTypeDic[entity.Tag].Remove(entity.Id);
            _entitiesDic.Remove(entity.Id);

            entity.Dispose();
        }

        public Entity GetEntityById(long id)
        {
            _entitiesDic.TryGetValue(id, out var entity);
            return entity;
        }

        /// <summary>
        /// 通过标签获取实体id
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<long> GetEntityIdByTag(EntityType tag)
        {
            _entityTypeDic.TryGetValue(tag, out var entityIds);
            return entityIds;
        }
        
#if UNITY_EDITOR
        public EntityViewer GetEntityViewer(long id)
        {
            _viewers.TryGetValue(id, out var viewer);
            return viewer;
        }

        private void OnUpdateEntityViewer(long id)
        {
            if (!_viewers.TryGetValue(id, out var viewer))
                return;

            viewer.UpdateInfo();
        }
#endif
    }
}