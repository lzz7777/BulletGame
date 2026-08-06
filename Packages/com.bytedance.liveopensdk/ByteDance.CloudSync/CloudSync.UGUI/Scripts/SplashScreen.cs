using System;
using ByteDance.CloudSync.UGUI;
using UnityEngine;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// Splash 启动屏元素构建器
    /// </summary>
    public interface ISplashScreenBuilder
    {
        void Init(ISplashScreenContainer screen);
    }

    /// <summary>
    /// 启动屏容器
    /// </summary>
    public interface ISplashScreenContainer
    {
        Canvas Canvas { get; }

        /// <summary>
        /// 关闭结束启动屏
        /// </summary>
        void Finish();
    }

    [DllMonoBehaviour]
    public class SplashScreen : UCloudView, ISplashScreen, ISplashScreenContainer
    {
        private ISplashScreenBuilder _builder;

        public static ISplashScreen Create(ISplashScreenBuilder builder = null)
        {
            if (builder == null)
                return null;
            var prefab = Resources.Load<GameObject>("SplashScreen");
            var instance = Instantiate(prefab);
            var provider = instance.GetComponent<SplashScreenProvider>();
            var view = instance.GetComponent<SplashScreen>();
            view.Factory = new UDeviceFactory(provider);
            view._builder = builder;
            return view;
        }

        public event Action CloseHandler;

        public IVirtualDeviceFactory Factory { get; private set; }

        public void OnOpen()
        {
            _builder.Init(this);
        }

        void ISplashScreen.OnClose()
        {
            gameObject.SetActive(false);
        }

        public Canvas Canvas => canvas;

        public void Finish()
        {
            CloseHandler?.Invoke();
            gameObject.SetActive(false);
        }
    }
}