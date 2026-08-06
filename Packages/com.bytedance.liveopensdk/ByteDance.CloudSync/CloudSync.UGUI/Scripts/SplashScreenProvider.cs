using ByteDance.CloudSync.UGUI;
using UnityEngine;

namespace ByteDance.CloudSync
{
    [DllMonoBehaviour]
    public class SplashScreenProvider : MonoBehaviour, ICloudViewProvider<UCloudView>
    {
        public SplashScreen view;

        public UCloudView CreateView(SeatIndex index)
        {
            return view;
        }
    }
}