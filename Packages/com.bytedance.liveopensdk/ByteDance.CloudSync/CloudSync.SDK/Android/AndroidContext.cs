using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ByteDance.CloudSync.CloudGameAndroid
{
    public static class AndroidContext
    {
        private static AndroidJavaClass _playerCls;
        private static AndroidJavaObject _currentActivity;
        private static AndroidJavaObject _applicationContext;
        private static AndroidJavaClass PlayerCls
        {
            get
            {
                
                if (_playerCls == null)
                {
                    _playerCls = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                }
                return _playerCls;
            }
        }
        private static AndroidJavaObject CurrentActivity
        {
            get 
            {
                if (_currentActivity == null)
                {
                    _currentActivity = PlayerCls.GetStatic<AndroidJavaObject>("currentActivity");
                }
                return _currentActivity;
            }
        }
        public static AndroidJavaObject ApplicationContext
        {
            get 
            {
                if (_applicationContext == null)
                {
                    _applicationContext = CurrentActivity.Call<AndroidJavaObject>("getApplicationContext");
                }
                return _applicationContext;
            }
        }
  
    }
}
