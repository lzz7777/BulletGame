using System;

namespace ByteDance.CloudSync
{
    internal class JoinRoomParam : ICloneable
    {
        /// <summary>
        /// 端上透传的InitParam。 是 json string 包括了全量的各个主播信息、嘉宾信息、匹配信息
        /// </summary>
        public string InitParam;

        /// 云游戏rtc的userid
        /// <remarks>注意：一般来说不等于抖音uid！</remarks>
        public string RTCUserId;

        /// <summary>
        /// 连屏房间 Id
        /// </summary>
        public string LinkRoomId;

        /// <summary>
        /// 查询结果, 如果不为 success 需要重试
        /// </summary>
        public ICloudGameAPI.ErrorCode Code;

        /// <summary>
        /// errorMsg 错误信息，跟随Code返回，用于调试排查
        /// </summary>
        public string Message;

        public JoinRoomParam()
        {
        }

        public JoinRoomParam(string initParam, string rtcUserId, string linkRoomId, ICloudGameAPI.ErrorCode code, string message = "")
        {
            InitParam = initParam;
            RTCUserId = rtcUserId;
            LinkRoomId = linkRoomId;
            Code = code;
            Message = message;
        }

        public void AcceptParam(ByteCloudGameSdk.JoinRoomParam param)
        {
            if (param == null)
                return;
            InitParam = param.InitParam;
            RTCUserId = param.RTCUserId;
            LinkRoomId = param.LinkRoomId;
            Code = param.Code.ToApiCode();
            Message = param.Message;
        }

        public ByteCloudGameSdk.JoinRoomParam ToSdkParam()
        {
            ByteCloudGameSdk.JoinRoomParam param = new ByteCloudGameSdk.JoinRoomParam();
            param.InitParam = InitParam;
            param.RTCUserId = RTCUserId;
            param.LinkRoomId = LinkRoomId;
            param.Code = Code.ToSdkCode();
            param.Message = Message;
            return param;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public class InputEventResponse
    {
        public int roomIndex;
        public string input;

        public InputEventResponse(int roomIndex, string input)
        {
            this.roomIndex = roomIndex;
            this.input = input;
        }
    }

    public enum ExitRoomReason
    {
        Unknown,
        Exit,
        Kickout,
    }

    public class ExitRoomParam : ICloneable
    {
        public ExitRoomReason Reason;
        public string RTCUserId;

        public ExitRoomParam()
        {
        }

        public ExitRoomParam(ExitRoomReason reason, string rtcUserId)
        {
            Reason = reason;
            RTCUserId = rtcUserId;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    internal interface IMultiplayerListener
    {
        void OnGameStart(string[] cloudRoomList);

        /// Sdk回调：RTC 进房事件, 保序.
        /// <para>注意：最新版本sdk：OnPlayerJoin 时 只信任 `int roomIndex` 和 `param.RTCUserId`，总是去做 Query</para>
        /// <para>note: 20240516 - db3ac8a - feat: 拆分 RTC 加房消息与长连接查询房间信息, 实现保序的加房退房消息回调能力</para>
        void OnPlayerJoin(int roomIndex, JoinRoomParam param);

        /// RTC 退房事件, 保序.
        void OnPlayerExit(int roomIndex, ExitRoomParam param);

        void OnPlayerOperate(int roomIndex, string opData);

        void OnTexturePush(long shareHandle);

        void OnPlayerInput(InputEventResponse res);

        void OnCustomMessage(int roomIndex, string msg);

        /// 查询房间消息回调
        void OnQueryRoomInfo(int roomIndex, JoinRoomParam param);
    }
}