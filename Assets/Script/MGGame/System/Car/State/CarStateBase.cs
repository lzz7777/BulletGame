using cfg;

namespace XN
{
    public abstract class CarStateBase
    {
        public long CarId { get; set; }
        public State CarState { get; set; }
        public CarViewComponent CarViewComponent => EntityManager.Instance.GetEntityById(CarId)?.GetComponent<CarViewComponent>();
        
        public virtual void OnEnter()
        {
            CarViewComponent.SwitchSpine(CarState);
        }

        public abstract void OnUpdate();

        public abstract void OnExit();
    }
}