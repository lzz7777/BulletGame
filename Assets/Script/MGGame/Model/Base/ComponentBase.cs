namespace XN
{
    public abstract class ComponentBase : IPool
    {
        public Entity Entity { get; set; }
        public bool IsFromPool { get; set; }
        
        // For O(1) removal from EntityManager._componentCache
        public int TypeIndex { get; set; } = -1;

        public virtual void OnCreate() { }
        public virtual void OnDestroy() { }
    }
}