using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ByteDance.CloudSync.CloudGameAndroid
{
    public class AccountScene : CloudGameScene
    {
        private static string TAG = "AccountScene";
        internal static AndroidJavaClass AccountSceneJavaClass { get; set; } = new AndroidJavaClass("com.bytedance.cloudplay.gamesdk.api.scene.AccountScene");
        internal AccountScene(AndroidJavaObject ajo) : base(ajo)
        {

        }
        public void RequestAuthToken()
        {
            LogUtils.WrapExceptionLog(() =>  SceneJavaObject.Call("requestAuthToken"), TAG);
        }
        /// <summary>
        /// 注册回调函数
        /// </summary>
        /// <param name="checkTokenCallBack">void(bool isSuccess)</param>
        public void SetSceneListener(CheckTokenCallBack checkTokenCallBack)
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("setSceneListener", new AccountSceneListener(checkTokenCallBack)), TAG);
        }
    }
}
