using System.Threading.Tasks;
using ByteDance.CloudSync.Mock;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ByteDance.CloudSync.Mock.Agent
{
    internal abstract class ServiceBehavior : WebSocketBehavior, IMessageChannel
    {
        public event ChannelMessageHandler OnMessageReceive;
        public MessageDelayer Delayer { get; set; }

        protected override async void OnMessage(MessageEventArgs e)
        {
            var w = JsonUtility.FromJson<MessageWrapper>(e.Data);
            if (Delayer != null)
                await Delayer.Delay(w.id);
            Loom.Run(() => OnMessageReceive?.Invoke(w));
        }

        public async void Send(MessageWrapper message)
        {
            if (Delayer != null)
                await Delayer.Delay(message.id);
            message.flags |= MessageFlags.Agent;
            var json = JsonUtility.ToJson(message);
            Sessions.SendTo(json, ID);
        }
    }
}