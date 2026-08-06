using UnityEngine.InputSystem;

namespace ByteDance.CloudSync
{
    internal class CustomGamepad : Gamepad, ICustomInputDevice
    {
        public override void MakeCurrent()
        {
        }

        public SeatIndex SeatIndex { get; set; }
    }
}