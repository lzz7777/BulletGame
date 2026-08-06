using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public enum InputSystemEvent
{
    /// <summary>
    /// 新增用户
    /// </summary>
    ChangesUser,

    /// <summary>
    /// 使用道具
    /// </summary>
    EventUseProp,

    /// <summary>
    /// 镜头滚动
    /// </summary>
    EventCameraRoller,
    EventCameraLift,

    /// <summary>
    /// 切换不同镜头
    /// </summary>
    EventSelectCamera,

    /// <summary>
    /// 切换选择不同玩家
    /// </summary>
    EventSelectPlayer,
    EventAddPlayer,

    /// <summary>
    /// 组间镜头
    /// </summary>
    EventAddBetweenGroup
}

public static class InputSystemTools
{
    /// <summary>
    /// </summary>
    /// <param name="map"></param>
    /// <param name="action"></param>
    public static void Enable(this InputActionMap map, params InputAction[] action)
    {
        foreach (var inputAction in map.actions)
            if (action.Contains(inputAction))
                inputAction.Enable();
            else
                inputAction.Disable();
    }
}

public class InputSystemManager : MonoSingleton<InputSystemManager>
{
    private InputSystemAction _inputActions;
    // private SelectCameraTypeSystem _selectCameraTypeSystem;

    public static InputSystemAction Input => Instance._inputActions;


    public ReadOnlyArray<InputActionMap> ActionMaps => _inputActions.asset.actionMaps;

    // private static OtherConfig OtherConfig => TotalConfigManager.ConfigManager.OtherConfig;

    /// <summary>
    /// 开启指定场景
    /// </summary>
    /// <param name="map"></param>
    public void Enable(params InputActionMap[] map)
    {
        var ids = new Guid[map.Length];
        for (var i = 0; i < ids.Length; i++) ids[i] = map[i].id;

        Enable(ids);
    }

    /// <summary>
    /// 开启指定场景
    /// </summary>
    /// <param name="ids"></param>
    public void Enable(params Guid[] ids)
    {
        foreach (var map in ActionMaps)
        {
            map.Disable();
            foreach (var guid in ids)
                if (map.id == guid)
                    map.Enable();
        }
    }

    private void InitAction()
    {
        // _inputActions.镜头.SelectPlayer.performed += OnSelectPlayer;
        _inputActions.用户.ADD.performed += OnChangesUser;
        // _inputActions.道具.SelectUser.performed += OnSelectPropUser;
        _inputActions.道具.Freed.performed += OnAddProp;
    }


    private void OnChangesUser(InputAction.CallbackContext ctx)
    {
        //判断测试按键开关
        // if (!OtherConfig.KeyboardSelf) return;
        var value = ctx.ReadValue<float>();
        Debug.Log($"OnChangesUser{value}");
        EventsManager.BroadCast(InputSystemEvent.ChangesUser, (int)value);
    }


    private void OnAddProp(InputAction.CallbackContext ctx)
    {
        //判断测试按键开关
        // if (!OtherConfig.KeyboardSelf) return;
        var value = ctx.ReadValue<float>();

        // var vales = GameData.AuthorityUserInfo.Values;
        var uids = new List<string>();

        if (uids.Count > 0)
        {
            var uid = uids[UnityEngine.Random.Range(0, uids.Count)];
            //userInfo.Uid, selectPropUser, (BuffType)value
            // if (GameData.TryGetUserInfo(uid, out var userInfo))
            // {
            //     var eCmd = (ECmd)value;
            //     if (eCmd == ECmd.buff分界)
            //     {
            //         CmdManager.Instance.UseProps(ECmd.buff2, userInfo.ID, 100, 100, DateTimeHelper.Timestamp);
            //     }
            //     else
            //     {
            //         CmdManager.Instance.UseProps(eCmd, userInfo.ID, 1, 1, DateTimeHelper.Timestamp);
            //     }
            // }
        }
    }


    #region 初始

    protected override void OnInit()
    {
        _inputActions = new InputSystemAction();
        InitAction();
        DefaultEnable();
    }

    private void DefaultEnable()
    {
        _inputActions.Disable();
        foreach (var map in ActionMaps) map.Disable();
    }

    protected override void OnRemove()
    {
        _inputActions.Disable();
    }

    #endregion
}