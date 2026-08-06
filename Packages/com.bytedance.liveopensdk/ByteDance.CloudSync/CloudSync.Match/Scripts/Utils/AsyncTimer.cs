using System;
using System.Threading;
using System.Threading.Tasks;
namespace ByteDance.CloudSync.Match
{
    public class AsyncTimer: IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
    
        public async void Start(int intervalMilliseconds, Action onTimer)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(intervalMilliseconds, token);
                    onTimer.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
                // Do nothing.
            }
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
}
