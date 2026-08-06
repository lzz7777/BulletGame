using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XN
{
    public enum GameModel
    {
        Debug,
        Release,
    }
    
    public class UIManager : MonoSingleton<UIManager>
    {
        private Stack<UIPanelBase> _panelStack = new();
        private Dictionary<string, UIPanelBase> _panelCache = new();
        [SerializeField][LabelText("游戏模式")]
        public GameModel GameModel = GameModel.Release;

        public Dictionary<UIPanelType, Transform> UiPanelCanvasDic = new();

        public GameObject UIRoot;
        private GameObject _poolRoot;
        public bool IsInitialized { get; private set; }

        protected override async void OnInit()
        {
            await UniTask.WaitUntil(() => YooAssetManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => TotalConfigManager.Instance.IsLoadOver);
            await UniTask.WaitUntil(() => ObjectPoolManager.Instance.IsInitialized);

            DontDestroyOnLoad(UIRoot);
            IsInitialized = true;
        }

        protected override void OnRemove()
        {
        }

        /// <summary>
        /// 判断ui是否打开
        /// </summary>
        /// <param name="uIWindowData"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool JudgeWindowOpen<T>(out T uIPanelBase) where T : UIPanelBase
        {
            uIPanelBase = null;
            string typeName = typeof(T).Name;

            if (!_panelCache.TryGetValue(typeName, out var window))
            {
                return false;
            }

            if (!window.gameObject.activeSelf)
            {
                return false;
            }
            
            uIPanelBase = (T)window;
            return true;
        }

        public async UniTask<UIPanelBase> OpenWindow<T>(UIWindowData uIWindowData = null) where T : UIPanelBase =>
            await OpenWindow(typeof(T).Name, uIWindowData);

        private async UniTask<UIPanelBase> OpenWindow(string typeName, UIWindowData uIWindowData = null, bool noPop = false)
        {
            if (!_panelCache.TryGetValue(typeName, out var window))
            {
                var obj = await ObjectPoolManager.Instance.GetFromPool(typeName, UiPanelCanvasDic[UIPanelType.Normal]);
                window = obj.GetComponent<UIPanelBase>();
                _panelCache[typeName] = window;
            }
            
            if (window.UIPanelType == UIPanelType.Normal)
            {
                //关闭上一个主页面
                if (_panelStack.Count > 0 && !noPop)
                {
                    var lastWindow = _panelStack.Peek();
                    string name = lastWindow.gameObject.name;
                    name = name.Substring(0, name.Length - 7);
                    await CloseWindow(name, true);
                }

                _panelStack.Push(window);
            }
            
            window.gameObject.SetActive(true);
            window.transform.SetParent(UiPanelCanvasDic[window.UIPanelType]);
            window.transform.SetAsLastSibling();

            var rt = (RectTransform)window.transform;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            window.OnOpen(uIWindowData);
            Debug.Log($"OpenWindow:{typeName}");
            
            return window;
        }

        public async UniTask CloseWindow<T>() where T : UIPanelBase => await CloseWindow(typeof(T).Name);

        private async UniTask CloseWindow(string typeName, bool noPop = false)
        {
            if (!_panelCache.TryGetValue(typeName, out var window) || !window.gameObject.activeSelf)
            {
                return;
            }
            
            window.gameObject.SetActive(false);
            window.OnClose();
            Debug.Log($"CloseWindow:{typeName}");
            
            if (window.UIPanelType == UIPanelType.Normal && _panelStack.Count > 0 && !noPop)
            {
                var nextWindow = _panelStack.Pop();
                string name = nextWindow.gameObject.name;
                name = name.Substring(0, name.Length - 7);
                await OpenWindow(name, null, true);
            }
        }
    }
}