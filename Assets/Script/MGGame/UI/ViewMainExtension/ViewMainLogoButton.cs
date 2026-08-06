using System;
using UnityEngine;
using UnityEngine.UI;
using XN;

public class ViewMainLogoButton : MonoBehaviour
{
    public Button LogoButton;


    private void Awake()
    {
        LogoButton.onClick.AddListener(this.OnLogoClick);
    }

    private void OnDestroy()
    {
        LogoButton.onClick.RemoveAllListeners();
    }

    private void OnLogoClick()
    {
        SensitiveManager.Refresh();
    }
}