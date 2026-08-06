using System.Threading.Tasks;
using StarkNetwork;

namespace ByteDance.CloudSync.Match
{
    public class ConnectionManager: IConnectionManager, IConnectionCallbacks
    {
        private readonly NetworkClient _client;
        private static DebugLogger Debug { get; } = new();

        private IConnectOption _option;
        
        // 重试重连相关
        private const int RetryInterval = 3;
        private const int MaxRetryTimes = 3;
        private int _crtRetryTimes = 0;

        private bool _isConnectingOrRetrying;
        private bool _requireConnection;
        
        public bool IsConnectingOrRetrying => _isConnectingOrRetrying;

        public ConnectionManager(NetworkClient client, IConnectOption option)
        {
            Debug.Log("ConnectionManager ctor");
            _client = client;
            _option = option;
            _client.AddCallbackTarget(this);
        }

        public void ConnectToServer()
        {
            if (_client.State != ClientState.STOP)
            {
                return;
            }
            DoConnect();
        }

        public void EnsureNetworkConnection(bool requireConnection)
        {
            Debug.Log($"EnsureNetworkConnection, [requireConnection]={requireConnection}");
            _requireConnection = requireConnection;
            if (requireConnection && _client.State == ClientState.STOP && !_isConnectingOrRetrying)
            {
                DoConnect();
            }
        }

        private void DoConnect()
        {
            _isConnectingOrRetrying = true;
            _client.ConnectUsingSettings(_option);
        }
        
        private async void Retry()
        {
            if (_crtRetryTimes >= MaxRetryTimes)
            {
                Debug.LogError( $"Retry failed for {MaxRetryTimes} times. Connect to gate failed.");
                _isConnectingOrRetrying = false;
                return;
            }

            _crtRetryTimes++;

            Debug.Log($"Will retry after {RetryInterval} seconds. Current retry count: [{_crtRetryTimes}/{MaxRetryTimes}]");
            await Task.Delay(1000 * RetryInterval);
            Debug.Log($"Retry now. Current retry count: [{_crtRetryTimes}/{MaxRetryTimes}]");
            DoConnect();
        }

        public void OnConnected(SerializedConnectResult result)
        {
            _isConnectingOrRetrying = false;
            _crtRetryTimes = 0;
        }

        public void OnConnectFailed(SerializedConnectFailedResult result)
        {
            Debug.Log( $"OnConnectFailed resumeMod: {result.resumeMod}, require: {_requireConnection}");
            if (_requireConnection)
            {
                Retry();
            }
            else
            {
                Debug.LogError( "Connect to gate failed.");
            }
        }

        public void OnDisconnected()
        {
            
        }

        public void OnConnectClosed(SerializedConnectCloseMessage msg)
        {
            Debug.Log( $"OnConnectClosed closeReason: {msg.closeReason} ({(int)msg.closeReason})");
            if (msg.closeReason == ConnectCloseReason.Initiative)
            {
                return;
            }
            if (_requireConnection)
            {
                Retry();
            }
        }

        public void OnPlayerInfoGot(SerializedPlayerCurrentInfo info)
        {
            
        }

        public void Dispose()
        {
            _requireConnection = false;
            _isConnectingOrRetrying = false;
            _client.CloseConnection();
            _client.RemoveCallbackTarget(this);
        }
    }
}