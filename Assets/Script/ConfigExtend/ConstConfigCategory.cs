namespace cfg.Global
{
    public partial class ConstConfigCategory
    {
        
        #region Json 化参数修改
        //场景 Game => UIManager => GameModel => GameModel.Debug
        public int DebugInt;
        public string HostAddress = DebugHost;
        #endregion

        #region Host
        /// <summary>
        /// 开发内网备用 莫文欢服务器  
        /// </summary>
        public const string LocalHost = "192.168.31.177";
        /// <summary>
        /// 开发时候编辑器下使用
        /// </summary>
        public const string DebugHost = "120.25.51.190";
        /// <summary>
        /// 正式服务器
        /// </summary>
        public const string OnlineHost = "120.78.122.76";
        #endregion
        // public bool DebugType => DebugInt==1;   // 用于显示调试
        public string Ver = "1.0.0";
        public ChannelCmd CurrChannel = ChannelCmd.DouYin;  // 需要调整手动  TODO在线参数覆盖
        public string SocketUrl => $"ws://{HostAddress}:30001";// Socket
        public string BaseUrl => $"http://{HostAddress}:30004"; // Http
    }
}
