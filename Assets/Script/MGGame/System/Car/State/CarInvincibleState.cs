namespace XN
{
    public class CarInvincibleState : CarStateBase
    {
        public override void OnEnter()
        {
            base.OnEnter();
            
            CarViewComponent.ViewCarInfoItem.OpenShield();
            CarViewComponent.ViewCarInfoItem.DoPlayShieldAnimation("fx_ui_ViewCarInfoItem_Shield_Normal");
        }

        public override void OnUpdate()
        {
        }

        public override void OnExit()
        {
            CarViewComponent.ViewCarInfoItem.DoPlayShieldBreakAnimation();
        }
    }
}