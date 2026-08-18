namespace XN
{
    public class CarFastState : CarStateBase
    {
        public override void OnEnter()
        {
            base.OnEnter();
            
            var carViewComp = CarViewComponent;
            carViewComp?.ViewCarInfoItem.DoPlayFastAnim();
        }

        public override void OnUpdate()
        {
            var carUnit = EntityManager.Instance.GetEntityById(CarId);
            var carInfoComp = carUnit.GetComponent<CarInfoComponent>();
            var carViewComp = carUnit.GetComponent<CarViewComponent>();
            var (remainTime, total1) = carInfoComp.StateDic[CarState];
            carViewComp?.ViewCarInfoItem.RefreshProgress(remainTime / total1);    
        }

        public override void OnExit()
        {
            var carUnit = EntityManager.Instance.GetEntityById(CarId);
            var carViewComp = carUnit.GetComponent<CarViewComponent>();
            carViewComp?.ViewCarInfoItem.DoCloseFastAnim();
            carViewComp?.ViewCarInfoItem.RefreshProgress();
        }
    }
}