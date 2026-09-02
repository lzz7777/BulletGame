using System.Collections.Generic;
using System;
using System.Threading;

namespace XN
{
    public class Entity : IPool
    {
        // 去掉属性封装，改为普通的 private static 字段
        private static long _nextId = 1000;
        public long Id { get; private set; }
        
        private Entity _parent { get; set; }
        public EntityType Tag { get; private set; }
        
        public bool IsFromPool { get; set; }
        
        private Dictionary<Type, IComponent> _components { get; set; } = new(64);
        private Dictionary<EntityType, List<Entity>> _childrenDic { get; set; } = new(128);

        public void Init(EntityType tag, bool isFromPool = false)
        {
            // 使用 Interlocked.Increment 保证绝对的原子性和多线程安全
            Id = Interlocked.Increment(ref _nextId);
            
            Tag = tag;
            IsFromPool = isFromPool;
        }

        public void Dispose()
        {
            Id = default;
            _parent = default;
            Tag = default;
            IsFromPool = default;
            _components.Clear();
            _childrenDic.Clear();
        }

        public bool IsDispose => Id == 0;

        public T AddComponent<T>(Action<T> onSetup = null, bool isFromPool = true) where T : IComponent, new()
        {
            T component;

            if (isFromPool)
            {
                component = EntityManager.Instance.GetFromPool<T>();
            }
            else
            {
                component = new T();
            }

            component.Entity = this;
            component.IsFromPool = isFromPool;
            _components[typeof(T)] = component;

            EntityManager.Instance.RegisterComponent(component);

            // 在调用 OnCreate 之前，先执行外部传入的赋值逻辑
            onSetup?.Invoke(component);
            
            component.OnCreate();

            return component;
        }

        public T GetComponent<T>() where T : IComponent
        {
            if (_components.TryGetValue(typeof(T), out var comp))
            {
                return (T)comp;
            }

            return null;
        }

        public bool GetComponent<T>(out T comp) where T : IComponent
        {
            if (_components.TryGetValue(typeof(T), out var com))
            {
                comp = (T)com;
                return true;
            }

            comp = null;
            return false;
        }

        public bool HasComponent<T>() where T : IComponent
        {
            return _components.ContainsKey(typeof(T));
        }

        public void RemoveComponent<T>(T comp) where T : IComponent
        {
            comp.OnDestroy();

            EntityManager.Instance.UnregisterComponent(comp);

            _components.Remove(comp.GetType());

            if (comp.IsFromPool)
            {
                EntityManager.Instance.ReturnToPool(comp);
            }
        }
        
        public Entity AddChild(EntityType entityType)
        {
            var child = EntityManager.Instance.CreateEntity(entityType);
            
            child._parent?.RemoveChild(child);

            if (!_childrenDic.TryGetValue(entityType, out var children))
            {
                _childrenDic.Add(entityType, children = new List<Entity>());
            }

            children.Add(child);
            child._parent = this;

#if UNITY_EDITOR
            EventsManager.BroadCast(GameEnum.UpdateEntityViewerEvent, child.Id);
#endif

            return child;
        }

        public void RemoveChild(Entity child)
        {
            if (!_childrenDic.TryGetValue(child.Tag, out var children))
            {
                Debug.LogError($"RemoveChild error _childrenDic no Tag {child.Tag}");
                return;
            }

            children.Remove(child);
            child._parent = null;
        }

        public Entity GetParent()
        {
            return _parent;
        }

        public Dictionary<EntityType, List<Entity>> GetChildren()
        {
            return _childrenDic;
        }

        public List<Entity> GetChildren(EntityType entityType)
        {
            if (_childrenDic.TryGetValue(entityType, out var children))
                return children;

            return null;
        }

        public Dictionary<Type, IComponent> GetAllComponents()
        {
            return _components;
        }

        public void OnDestroy()
        {
            foreach (var comp in _components.Values)
            {
                comp.OnDestroy();

                EntityManager.Instance.UnregisterComponent(comp);

                if (comp.IsFromPool)
                {
                    EntityManager.Instance.ReturnToPool(comp);
                }
            }

            _components.Clear();
        }
    }
}