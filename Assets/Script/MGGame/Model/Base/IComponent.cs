namespace XN
{
    public abstract class IComponent : IPool
    {
        public Entity Entity { get; set; }
        public bool IsFromPool { get; set; }
        
        // For O(1) removal from EntityManager._updateComponents
        public int UpdateIndex { get; set; } = -1;
        
        // For O(1) removal from EntityManager._componentCache
        public int TypeIndex { get; set; } = -1;

        public virtual void OnCreate() { }
        public virtual void OnDestroy() { }
    }
}