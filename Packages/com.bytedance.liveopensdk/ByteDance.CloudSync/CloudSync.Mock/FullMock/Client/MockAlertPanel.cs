using System;
using UnityEngine;
using UnityEngine.UI;

namespace ByteDance.CloudSync.Mock
{
    [Flags]
    public enum AlertButtons
    {
        None = 0x0,
        Ok = 0x1,
        Cancel = 0x2
    }

    [DllMonoBehaviour]
    public class MockAlertPanel : MockPopPanel
    {
        [SerializeField]
        private Text textTitle;
        [SerializeField]
        private Text textContent;
        [SerializeField]
        private Button buttonOk;

        private void Awake()
        {
            buttonOk.onClick.AddListener(OnClickOk);
        }

        private void OnClickOk()
        {
            Hide();
        }

        public void Show(string title, string content, AlertButtons buttons = AlertButtons.Ok | AlertButtons.Cancel)
        {
            textTitle.text = title;
            textContent.text = content;
            gameObject.SetActive(true);

            buttonOk.gameObject.SetActive(buttons.HasFlag(AlertButtons.Ok));
        }
    }
}