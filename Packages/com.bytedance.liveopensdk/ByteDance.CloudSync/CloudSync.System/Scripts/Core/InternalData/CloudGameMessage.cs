namespace ByteDance.CloudSync
{
    internal class PlayerConnectingMessage : CloudGameMessageBase
    {
        public override string ToString()
        {
            return $"连接中 {UserInfo}";
        }
    }

    internal class PlayerConnectedMessage : CloudGameMessageBase
    {
        public override string ToString()
        {
            return $"建立连接成功 {UserInfo}";
        }
    }

    internal class PlayerDisconnectedMessage : CloudGameMessageBase
    {
        public override string ToString()
        {
            return $"断开连接成功 {UserInfo}";
        }
    }

    /// <summary>
    /// 用户输入操作（鼠标、键盘、触摸等）
    /// </summary>
    internal class PlayerOperateMessage : CloudGameMessageBase
    {
        public PlayerOperate operateData;
        public override string ToString()
        {
            return null;
        }
    }

    internal class PlayerCustomMessage : CloudGameMessageBase
    {
        /// <summary>
        /// 长连自定义消息的内容
        /// </summary>
        public string message;

        public override string ToString()
        {
            return message;
        }
    }
}
