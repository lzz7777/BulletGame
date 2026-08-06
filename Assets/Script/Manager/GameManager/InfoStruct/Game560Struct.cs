// using System;
// using System.Threading;
// using BestHTTP.JSON.LitJson;
// using Cysharp.Threading.Tasks;
//
// namespace InfoStruct
// {
//     public static class Game560Deserialize
//     {
//         private static int _pingPongNum;
//         private static CancellationTokenSource cts;
//         private static CmdManager Cmd => CmdManager.Instance;
//
//         public static async void PingPong(SocketManager socket)
//         {
//             cts?.Cancel();
//             cts = new CancellationTokenSource();
//             _pingPongNum = 0;
//             while (++_pingPongNum < 10)
//             {
//                 socket.Send("ping");
//                 try
//                 {
//                     await UniTask.Delay(1000, cancellationToken: cts.Token);
//                 }
//                 catch (Exception e)
//                 {
//                     return;
//                 }
//             }
//             // await UniTask.Delay(1000, cancellationToken: cts.Token);
//
//             socket.CloseSocket(1001, "心跳包超时");
//         }
//
//         public static void Deserialize(string receivedMsg)
//         {
//             var item = JsonMapper.ToObject<Game560Code>(receivedMsg);
//             if (item.MsgType is 2 or 3 or 4)
//             {
//                 var info = JsonMapper.ToObject<Game560Info>(receivedMsg);
//
//                 Cmd.UpdateUserInfo(info.UserId.ToString(), info.Username, info.HeadImage);
//                 var timestamp = (long)DateTimeHelper.TimestampMs;
//                 switch (info.MsgType)
//                 {
//                     case 4: //聊天消息
//                     {
//                         var chat = JsonMapper.ToObject<Game560Chat>(receivedMsg);
//                         Cmd.ChatMessage(chat.Content, chat.UserId.ToString(), timestamp);
//                         break;
//                     }
//                     case 2: //送礼
//                     {
//                         var gift = JsonMapper.ToObject<Game560Gift>(receivedMsg);
//                         Cmd.GiftMessage(gift.GiftName, gift.GiftId.ToString(), gift.GiftNum, gift.UserId.ToString(),
//                             gift.GiftNum * gift.GiftPrice, timestamp);
//                         break;
//                     }
//                     case 3: //点赞 无人机
//                     {
//                         var like = JsonMapper.ToObject<Game560Like>(receivedMsg);
//                         Cmd.LikeMessage(like.UserId.ToString(), like.Count, timestamp);
//                         break;
//                     }
//                 }
//             }
//             else if (item.MsgType == 7)
//             {
//                 //Application.Quit();
//             }
//             else if (item.MsgType == 8)
//             {
//                 _pingPongNum = 0;
//             }
//         }
//     }
//
//     public class Game560PingPong
//     {
//         public readonly string Message = "ping";
//     }
//
//     public struct Game560Code : Game560Base
//     {
//         public int MsgType { get; set; }
//     }
//
//     public struct Game560Gift : Game560InfoBase
//     {
//         public int MsgType { get; set; }
//         public long UserId { get; set; }
//         public string HeadImage { get; set; }
//         public string Username { get; set; }
//         public readonly long GiftId;
//         public readonly int GiftNum;
//         public readonly int GiftPrice;
//         public readonly string GiftName;
//         public readonly string GiftImage;
//     }
//
//     public struct Game560Like : Game560InfoBase
//     {
//         public int MsgType { get; set; }
//         public long UserId { get; set; }
//         public string HeadImage { get; set; }
//         public string Username { get; set; }
//         public readonly int Count;
//         public readonly int Total;
//     }
//
//     public struct Game560Chat : Game560InfoBase
//     {
//         public int MsgType { get; set; }
//         public long UserId { get; set; }
//         public string HeadImage { get; set; }
//         public string Username { get; set; }
//         public readonly string Content;
//     }
//
//     public struct Game560Info : Game560InfoBase
//     {
//         public int MsgType { get; set; }
//         public long UserId { get; set; }
//         public string HeadImage { get; set; }
//         public string Username { get; set; }
//     }
//
//     public interface Game560InfoBase : Game560Base
//     {
//         public int MsgType { get; set; }
//         public long UserId { get; set; }
//         public string HeadImage { get; set; }
//         public string Username { get; set; }
//     }
//
//     public interface Game560Base
//     {
//         public int MsgType { get; set; }
//     }
// }