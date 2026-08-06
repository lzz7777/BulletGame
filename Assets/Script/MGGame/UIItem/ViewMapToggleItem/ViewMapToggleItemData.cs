using System;
using UnityEngine.UI;

namespace XN
{
public class ViewMapToggleItemData
{
    public int SceneId;
    public Action<int> OnClick;
    public ToggleGroup toggleGroup;
    public bool isOn = false;
}
}
