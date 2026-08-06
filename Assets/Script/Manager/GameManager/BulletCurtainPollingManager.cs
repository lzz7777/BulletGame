using System;
using System.Collections.Generic;
using System.Text;
using cfg;
using cfg.Global;
using cfg.Net;
/*
using GameMain;
using InfoStruct;
// using RabbitMQ.Client;
// using RabbitMQ.Client.Events;
using UnityEngine;

namespace Base
{
    public class BulletCurtainPollingManager : MonoSingleton<BulletCurtainPollingManager>
    {
        private static string _exchangeName = "live_game_combat_exchange";
        private static string _routingKey = "live_game_routing_key";
        private static string _queueName = "";

        // 创建连接工厂
        private ConnectionFactory _factory = new()
        {
            HostName = "134.175.0.207",
            UserName = "xy_live_game", // 默认用户名
            Password = "Xysz2023", // 默认密码
            Port = 5672, // 默认端口
            VirtualHost = "/live_game" // 默认虚拟机
        };

        private readonly Queue<string> _msgQueue = new();
        private IModel _channel;

        protected override void OnInit()
        {
        }

        protected override void OnRemove()
        {
            Debug.Log("断开MQ");
            _channel?.Close();
            _channel = null;
        }

        /// <summary>
        /// 生产者
        /// </summary>
        /// <param name="message">消息信息</param>
        public void Producer(string message)
        {
            // 获取TCP长链接
            using (var connection = _factory.CreateConnection())
            {
                // 创建通道
                using (var channel = connection.CreateModel())
                {
                    // 声明队列
                    channel.QueueDeclare(_queueName, true, false, false,
                        null);
                    // 将消息转换成字节数组
                    var body = Encoding.UTF8.GetBytes(message);
                    // 发送消息到队列
                    channel.BasicPublish(_exchangeName,
                        _routingKey, null,
                        body);
                    Debug.Log($"Producer message: {message}");
                }
            }
        }

        /// <summary>
        /// 消费者
        /// </summary>
        public void Consumer(in GaCombatConfigVo config)
        {
            _factory = new ConnectionFactory
            {
                HostName = config.host,
                UserName = config.name,
                Password = config.pwd,
                Port = config.port,
                VirtualHost = config.virtualHost
            };

            _exchangeName = config.exchange;
            _routingKey = config.routingKey;
            _queueName = config.queueName ?? "";

            // 建立连接和通道
            // 消费者持续运行以侦听消息，不使用 using 来释放资源
            var connection = _factory.CreateConnection();
            _channel = connection.CreateModel();
            _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct);
            //由于消费者可能先于生产者启动，所以消费者也需要声明队列
            _channel.QueueDeclare(_queueName, true, false, false, null);
            _channel.QueueBind(_queueName, _exchangeName, _routingKey);
            // 创建消费者
            var consumer = new EventingBasicConsumer(_channel);
            // 接收到消息后的处理方法
            consumer.Received += (model, eventArgs) =>
            {
                // 获取消息
                var body = eventArgs.Body.ToArray();
                var receivedMsg = Encoding.UTF8.GetString(body);
                if (receivedMsg != "test!!")
                    lock (_msgQueue)
                    {
                        _msgQueue.Enqueue(receivedMsg);
                    }

                // 处理消息
                Debug.Log($"Received message: {receivedMsg}");
                // 消息确认
                _channel.BasicAck(eventArgs.DeliveryTag, false);

                Debug.Log($"Bullet OnMessageReceived:{receivedMsg}");
            };

            // 消费者开启监听，从队列中获取消息
            // 为了确保消息不会丢失，RabbitMQ支持消息应答。消费者发回一个ack（确认），告诉RabbitMQ一个特定的消息已经被接收、处理，RabbitMQ可以自由删除它。
            _channel.BasicConsume(_queueName, false, consumer);
        }

        #region 数据处理

        private static LoginInfoConfigCategory ChannelConfig => TotalConfigManager.ConfigManager.LoginInfoConfigCategory;
        private static ConstConfigCategory ConstConfig => TotalConfigManager.ConfigManager.ConstConfigCategory;

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyUp(KeyCode.L)) Producer("test!!");
#endif
            if (!XN.GameStateCtrl.IsGameAllState) return;

            LoginInfoConfig loginInfo = ChannelConfig.GetOrDefault(ConstConfig.CurrChannel);
            if (loginInfo.Channel is ChannelCmd.DouYin) DyDeserialize.UpdateFailStatus();

            lock (_msgQueue)
            {
                while (_msgQueue.Count > 0)
                {
                    var receivedMsg = _msgQueue.Dequeue();
                    try
                    {
                        if (string.IsNullOrEmpty(receivedMsg)) return;

                        // if (loginInfo.Channel is ChannelCmd.快手)
                        //     KsDeserialize.Deserialize(receivedMsg);
                        // else if (loginInfo.Channel is ChannelCmd.SUD)
                        //     SudDeserialize.Deserialize(receivedMsg);
                        // else 
                        if (loginInfo.Channel is ChannelCmd.DouYin) DyDeserialize.Deserialize(receivedMsg);
                        // DyDeserialize.UpdateFailStatus();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        throw;
                    }
                }
            }
        }

        #endregion
    }
}

*/