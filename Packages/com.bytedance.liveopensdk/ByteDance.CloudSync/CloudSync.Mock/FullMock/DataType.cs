using System;
using System.Linq;
using ByteDance.CloudSync.Mock.Agent;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Serialization;

namespace ByteDance.CloudSync.Mock
{
    public static class MessageId
    {
        public const int ErrorNotify = -1;

        public const int Offer = 1;
        public const int Answer = 2;
        public const int Candidate = 3;
        public const int MouseInput = 4;
        public const int KeyboardInput = 5;
        public const int TouchInput = 6;

        public const int JoinRoom = 10;
        public const int ExitRoom = 11;
        public const int JoinRoomNotify = 12;

        public const int MatchReq = 20;
        public const int MatchResp = 21;
        public const int CancelMatchReq = 22;
        public const int CancelMatchResp = 23;
        public const int EndGameReq = 24;
        public const int EndGameNotify = 25;
        public const int PodMessageReq = 26;
        public const int PodMessageNotify = 27;
    }

    [Flags]
    public enum MessageFlags
    {
        Room = 0b1,
        Client = 0b10,
        Agent = 0b100,
    }

    [Serializable]
    public class MessageWrapper
    {
        public MessageFlags flags;
        public int id;
        public string sessionId;
        public string data;

        private MessageWrapper()
        {
        }

        public T To<T>() => JsonUtility.FromJson<T>(data);

        public bool IsRoomMessage => (flags & MessageFlags.Room) != 0;

        public bool IsAgentMessage => (flags & MessageFlags.Agent) != 0;

        public static MessageWrapper CreateNotify(int id, string message)
        {
            return new MessageWrapper
            {
                id = id,
                data = message,
            };
        }

        public static MessageWrapper CreateRequest(int id, string message)
        {
            return new MessageWrapper
            {
                id = id,
                data = message,
            };
        }
    }

    [Serializable]
    public class DescObject
    {
        public RTCSdpType type;
        public string sdp;

        public static DescObject From(RTCSessionDescription sessionDescription)
        {
            return new DescObject
            {
                type = sessionDescription.type,
                sdp = sessionDescription.sdp,
            };
        }

        public RTCSessionDescription To()
        {
            return new RTCSessionDescription
            {
                type = type,
                sdp = sdp,
            };
        }
    }

    [Serializable]
    public class CandidateObject
    {
        public string candidate;
        public string sdpMid;
        public int? sdpMLineIndex;

        public static CandidateObject From(RTCIceCandidate candidate)
        {
            return new CandidateObject
            {
                candidate = candidate.Candidate,
                sdpMid = candidate.SdpMid,
                sdpMLineIndex = candidate.SdpMLineIndex,
            };
        }

        public RTCIceCandidate To()
        {
            var init = new RTCIceCandidateInit
            {
                candidate = candidate,
                sdpMid = sdpMid,
                sdpMLineIndex = sdpMLineIndex,
            };
            return new RTCIceCandidate(init);
        }
    }

    /// <summary>
    /// Rtc房间参数，用于连接到目标Rtc房间
    /// </summary>
    public class RtcConnectOptions
    {
        public string Host = AgentServer.Host;
        public int Port = AgentServer.Port;

        /// <summary>
        /// 想要连接的Rtc房间ID
        /// </summary>
        public string RoomId;

        /// <summary>
        /// 想要连接的房主的PodToken（HostToken）
        /// </summary>
        public string PodToken;
    }

    [Serializable]
    public class ErrorNotify
    {
        public string title;
        public string content;
    }

    /// <summary>
    /// RtcMock鼠标数据（仅内部）
    /// </summary>
    [Serializable]
    public class RtcMouseData
    {
        public MouseAction action;
        public MouseButtonId button;
        /// 左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高
        public Vector2 point;
        public Vector2Int screenSize;
        public double wheel;
    }

    [Serializable]
    public class RtcTouchData
    {
        public RtcTouch[] touches;
        public Vector2Int screenSize;

        public static RtcTouchData Create(Touch[] touches, Vector2Int screenSize)
        {
            var rtcTouches = touches.Select(RtcTouch.Create).ToArray();
            return new RtcTouchData
            {
                touches = rtcTouches,
                screenSize = screenSize,
            };
        }

        public Touch[] ToUnityTouches()
        {
            return touches.Select(s => s.ToUnityTouch()).ToArray();
        }
    }

    /// 对齐 Unity Touch 的数据
    [Serializable]
    public class RtcTouch
    {
        public int fingerId;
        /// 左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高
        public Vector2 position;
        public Vector2 deltaPosition;
        public TouchPhase phase;

        public static RtcTouch Create(Touch touch)
        {
            return new RtcTouch
            {
                fingerId = touch.fingerId,
                position = touch.position,
                deltaPosition = touch.deltaPosition,
                phase = touch.phase,
            };
        }

        public Touch ToUnityTouch()
        {
            // `UnityEngine.Touch` position 左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高
            return new Touch
            {
                fingerId = fingerId,
                position = position,
                rawPosition = position,
                deltaPosition = deltaPosition,
                phase = phase,
            };
        }
    }

    [Serializable]
    public class RtcKeyboardData
    {
        public KeyboardAction action;
        public KeyCode keyCode;
    }

    [Serializable]
    internal class RtcJoinRoom
    {
        public string rtcUserId;
        public SeatIndex index;
        public bool isLocalDevice;
    }

    [Serializable]
    internal class RtcJoinRoomNotify
    {
        public SeatIndex index;
    }

    [Serializable]
    internal class RtcExitRoom
    {
        public string rtcUserId;
    }

    [Serializable]
    internal class RtcPodMessageReq
    {
        public string targetToken;
        public string extraInfo;
    }

    [Serializable]
    internal class RtcPodMessageNotify
    {
        public string extraInfo;
    }
}