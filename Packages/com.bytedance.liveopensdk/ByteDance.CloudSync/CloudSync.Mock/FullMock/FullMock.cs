using ByteDance.CloudSync.Mock.Agent;

namespace ByteDance.CloudSync.Mock
{
    /// <summary>
    /// 全面的 Mock 模拟方案。
    /// 可以在本地或局域网中模拟调试多个用户的画面、输入、和匹配同玩流程，有接近真实环境的RTC推流。
    /// </summary>
    /// <remarks>
    /// FullMock Rtc链路关系： Mock玩法窗口 <see cref="MockPlay"/> 拉流和发送输入 -- 客户端Rtc流 <see cref="ClientRtc"/> -- Agent服务器 <see cref="Agent.AgentServer"/> -- 云端Pod实例 <see cref="PodInstance"/> 推流和接收云游戏事件 <br/>
    /// <br/>
    /// FullMock 启动运行流程：  <br/>
    /// * MockPlay玩法窗口运行，启动各个Mock模块 <see cref="MockPlay.Launch"/>  <br/>
    /// * 服务器启动 <see cref="Agent.AgentServer.Start"/>  <br/>
    /// * 云游戏实例Pod启动 <see cref="PodInstance.Start"/>  <br/>
    /// * Pod 连接 Rtc房间服务 <see cref="PodInstance.Connect"/> <see cref="PodRtcRoomService"/>  <br/>
    /// * Pod 连接 Rtc房间服务 成功 <see cref="PodRtcRoomService.OnOpen"/>  <br/>
    /// * 客户端 切流连接 Rtc服务 <see cref="ClientRtc.Connect"/> <see cref="ClientRtcService"/>  <br/>
    /// * 客户端 切流连接 Rtc服务 成功 <see cref="ClientRtcService.OnOpen"/>  <br/>
    /// * 触发 Rtc进房 <see cref="PodRtcRoomService.JoinRoom"/>  <br/>
    /// * Pod 收到进房事件 <see cref="PodInstance.HandleAgentMessage"/>  <br/>
    /// * 云同步SDK收到进房事件，开始推流 <see cref="VirtualScreen.OnCameraRender"/>  <br/>
    /// * MockPlay拉流 <see cref="MockPlay.UpdateTexture"/>  <br/>
    /// </remarks>
    public static class FullMock
    {
        /// <summary>
        /// FullMock启动
        /// </summary>
        public static void Setup()
        {
            MockPlay.Setup();
            RtcMock.Setup();
        }
    }
}