using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace XN
{
public class ViewTicker : UIPanelBase
{
    public Image UIFrameImage;

	public override void OnOpen(UIWindowData uIWindowData)
	{
		base.OnOpen();
		this.OnOpenSystem(uIWindowData);
	}

	public override void OnClose()
	{
		base.OnClose();
		this.OnCloseSystem();
	}

	#region CustomFields

	public Queue<ViewTickerItemData> Datas = new();
	public List<GameObject> Items = new();
	public float Time = 0;
	public float ExistTime = 2;
	public float MoveTime = 1;
	
	private void Update()
	{
		this.OnUpdateSystem();
	}

	#endregion
}
}
