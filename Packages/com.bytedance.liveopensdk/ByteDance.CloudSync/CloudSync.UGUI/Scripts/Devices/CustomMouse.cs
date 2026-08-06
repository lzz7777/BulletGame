using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace ByteDance.CloudSync
{
    internal interface ICustomInputDevice
    {
        SeatIndex SeatIndex { get; set; }
    }

    [InputControlLayout(canRunInBackground = true)]
    internal class CustomMouse : Mouse, ICustomInputDevice
    {
        public override void MakeCurrent()
        {
        }

        public SeatIndex SeatIndex { get; set; }
    }
}
