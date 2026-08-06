namespace XN
{
    public class CarPositionComponent : IComponent
    {
        public float X { get; set; }
        public float Y { get; set; }
        
        public float MoveXTime { get; set; }
        public float MoveYTime { get; set; }
        
        public bool InitPos { get; set; }
        
        public override void OnCreate()
        {
            MoveXTime = 0.4f;
        }

        public override void OnDestroy()
        {
        }
    }
}