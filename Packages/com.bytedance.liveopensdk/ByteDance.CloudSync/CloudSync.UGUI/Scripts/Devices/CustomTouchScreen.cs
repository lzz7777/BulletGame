using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace ByteDance.CloudSync
{
    [InputControlLayout(canRunInBackground = true)]
    internal class CustomTouchscreen : Touchscreen, ICustomInputDevice
    {
        public override void MakeCurrent()
        {
        }

        public SeatIndex SeatIndex { get; set; }
    }
}
