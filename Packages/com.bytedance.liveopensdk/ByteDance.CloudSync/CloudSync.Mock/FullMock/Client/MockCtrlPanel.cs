using System;
using UnityEngine;
using UnityEngine.UI;

namespace ByteDance.CloudSync.Mock
{
    /// <summary>
    /// 控制面板
    /// </summary>
    [DllMonoBehaviour]
    public class MockCtrlPanel : MockPopPanel
    {
        [SerializeField] private Text textContent;
        [SerializeField] private Button buttonLeave;
        [SerializeField] private Button buttonClose;
        [SerializeField] private Toggle toggleSimTouchByMouse;

        private void Awake()
        {
        }

        private void Start()
        {
            textContent.text = null;
            buttonClose.onClick.AddListener(OnClickClose);
            buttonLeave.onClick.AddListener(OnClickLeave);
            toggleSimTouchByMouse.onValueChanged.AddListener(v => MockPlay.Instance.SimulateTouchByMouse = v);
            toggleSimTouchByMouse.isOn = MockPlay.Instance.SimulateTouchByMouse;
        }

        private void OnClickClose()
        {
            Hide();
        }

        private void OnClickLeave()
        {
            Hide();
            MockPlay.Instance.MockDisconnect();
        }
    }
}