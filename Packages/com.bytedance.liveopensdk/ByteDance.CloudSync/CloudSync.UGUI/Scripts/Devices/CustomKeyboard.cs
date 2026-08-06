using UnityEngine.InputSystem;

namespace ByteDance.CloudSync
{
    internal class CustomKeyboard : Keyboard, ICustomInputDevice
    {
        public override void MakeCurrent()
        {
        }

        public SeatIndex SeatIndex { get; set; }
    }
}
