using System;
using System.Text;
using BestHTTP;
using cfg;
using Cysharp.Threading.Tasks;
using GameMain;
using InfoStruct;
using SimpleJSON;
using Sirenix.OdinInspector;
using UnityEngine;

public class DebugManager : MonoSingleton<DebugManager>
{
    
    
    // [SerializeField] [LabelText("测试弹出全屏公告的时间戳")]
    // public long debugDisplayRanking;
    //
    // [SerializeField] [LabelText("按键加入是否测试排名")]
    // public bool debugRank;
    //
    // [SerializeField] [LabelText("按键加入是否测试连胜")]
    // public bool debugContinueWin;

    protected override void OnInit()
    {
        // TODO 读取后续配置在线参数 可以切换当前渠道 currChannel
        
    }

    protected override void OnRemove()
    {
    }
#if UNITY_EDITOR

    [SerializeField] [LabelText("服务器token")]
    public string LocalToken;

    // [Header("礼物测试")] public InteractiveID giftId;
    // public int giftCount = 1;
    // public string uid = "888";
    // public string userName = "888";
    //
    // [Button("测试礼物")]
    // private void TestGift()
    // {
    //     CmdManager.Instance.UpdateUserInfo(uid, userName, TotalConfigManager.GetHand());
    //     CmdManager.Instance.GiftMessage("", ((int)giftId).ToString(), giftCount, uid, (long)DateTimeHelper.TimestampMs);
    // }
    //
    // [Header("聊天测试")] public string testChat = "";
    //
    // [Button("测试聊天")]
    // private void TestChat()
    // {
    //     if (!string.IsNullOrEmpty(testChat))
    //     {
    //         CmdManager.Instance.UpdateUserInfo(uid, userName, "");
    //
    //         CmdManager.Instance.ChatMessage(testChat, uid, (long)DateTimeHelper.TimestampMs);
    //     }
    // }
    //
    // [Button("测试点赞")]
    // private void TestLike()
    // {
    //     CmdManager.Instance.UpdateUserInfo(uid, userName, "");
    //     CmdManager.Instance.LikeMessage(uid, 1, (long)DateTimeHelper.TimestampMs);
    // }
    //
    // [Button("测试连续点赞")]
    // private async void TestMaxLike()
    // {
    //     var u = uid;
    //     var un = userName;
    //     while (true)
    //     {
    //         CmdManager.Instance.UpdateUserInfo(u, un, "");
    //         CmdManager.Instance.LikeMessage(u, 1, (long)DateTimeHelper.TimestampMs);
    //         await UniTask.DelayFrame(60);
    //     }
    // }
    
    [SerializeField] [LabelText("游戏渠道切换")]
    public ChannelCmd currChannel = ChannelCmd.DouYin;  // 先与TotalConfigManager初始化
    public string testJson = "";

    [Button("@\"测试Json: \" + currChannel.ToString()")]
    private void TestJson()
    {
        if (!string.IsNullOrEmpty(testJson))
            switch (currChannel)
            {
                // case ChannelCmd.微信:
                //     break;
                // case ChannelCmd.快手:
                //     KsDeserialize.Deserialize(testJson);
                //     break;
                // case ChannelCmd.SUD:
                //     SudDeserialize.Deserialize(testJson);
                //     break;
                case ChannelCmd.DouYin:
                    DyDeserialize.Deserialize(testJson);
                    break;
                // case ChannelCmd.Game560:
                //     Game560Deserialize.Deserialize(testJson);
                    break;
            }
    }

    //{"code":0,"data":{"data_list":[{"msg_type":"live_gift","payload":"[{\"msg_id\":\"7379659605624083510\",\"sec_openid\":\"_000vEcGfQZ3SfybiymmFzHcPrkbyUZzIiBz\",\"sec_gift_id\":\"rROiXLcY2saGvxHt3fAkYbWvbbikhEzbo0wpI794zEv+A2SCLrkNKYZEVuE=\",\"gift_num\":1,\"gift_value\":30000,\"avatar_url\":\"https://p11.douyinpic.com/aweme/100x100/aweme-avatar/tos-cn-i-0813_o0NSG6HAAAChAJg1BazYCyIYoEAjQEeRwfRABS.jpeg?from=3067671334\",\"nickname\":\"大鼻涕泡泡\",\"timestamp\":1718210922000}]","room_id":"7379647559584631604"}],"page_num":1,"total_count":1},"message":null}
    [Button("@\"测试FailJson: \" + currChannel.ToString()")]
    private void TestFailJson()
    {
        if(string.IsNullOrEmpty(testJson)) return;
        DyDeserialize.DeserializeFailItem(testJson);
    }


    [Button("清理缓存")]
    private void ClearC()
    {
        SaveData.DeleteAll();
    }

    private static HTTPRequest _request;
    [Button("测试上传日志")]
    private void UpLoad()
    {
        if (_request == null)
        {
            var url = "http://localhost:8000/upload";
            _request = new HTTPRequest(new Uri(url), HTTPMethods.Post, (_, _) => { });
            _request.SetHeader("Content-Type", "application/text");
            _request.SetHeader("X-Folder-Name", Application.productName);
            _request.SetHeader("X-File-Name", "log.bbb");
        }

        _request.ClearForm();
        var jsonData = new JSONObject
        {
            ["type"] = "上报错误日志",
            ["time"] = $"{DateTimeHelper.NowServer:MM-dd- HH:mm:ss}",
        };

        var json = jsonData.ToString();
        _request.RawData = Encoding.Default.GetBytes(json);
        _request.Send();
    }
#endif
}