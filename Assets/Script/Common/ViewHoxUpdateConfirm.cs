using System;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    public class ViewHoxUpdateConfirm : MonoBehaviour
    {
        public Text content;
        public Button confirmButton;

        public Action OnConfirm;
        
        private void Awake()
        {
            confirmButton.onClick.AddListener(ConfirmButtonOnClick);
        }

        private void ConfirmButtonOnClick()
        {
            OnConfirm?.Invoke();
        }
    }
}