using System.Collections.Generic;
using System;

namespace XN
{
    public class Entity : IPool
    {
        private static long _nextId { get; set; } = 1000;
        public long Id { get; private set; }
        private Entity _parent { get; set; }
        public EntityType Tag { get; private set; }
        
        public bool IsFromPool { get; set; }
        
        private Dictionary<Type, IComponent> _components { get; set; } = new();
        private List<Entity> _children { get; set; } = new();

        public void Init(EntityType tag, bool isFromPool = false)
        {
            Id = ++_nextId;
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
            _children.Clear();
        }

        public bool IsDispose => Id == 0;

        public T AddComponent<T>(bool isFromPool = true) where T : IComponent, new()
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

            _children.Add(child);
            child._parent = this;

#if UNITY_EDITOR
            EventsManager.BroadCast(GameEnum.UpdateEntityViewerEvent, child.Id);
#endif

            return child;
        }

        public void RemoveChild(Entity child)
        {
            _children.Remove(child);
            child._parent = null;
        }

        public Entity GetParent()
        {
            return _parent;
        }

        public List<Entity> GetChildren()
        {
            return _children;
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