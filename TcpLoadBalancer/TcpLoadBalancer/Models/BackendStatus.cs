
namespace TcpLoadBalancer.Models
{
    public class BackendStatus
    {
        public BackendEndpoint Endpoint { get; init; } = null!;
        public bool IsHealthy { get; set; } = true;
        public bool IsEnable { get; set; } = true;

        private int _activeConnections;
        public int ActiveConnections => _activeConnections;

        private readonly object _lock = new object();
        public CancellationTokenSource? ConnectionCancellationTokenSource { get; private set; }

        public void IncrementActiveConnections() => Interlocked.Increment(ref _activeConnections);

        public void DecrementActiveConnections()
        {
            if (_activeConnections > 0)
                Interlocked.Decrement(ref _activeConnections);
        }

        public void ResetActiveConnections()
        {
            Interlocked.Exchange(ref _activeConnections, 0);

            lock (_lock)
            {
                if (ConnectionCancellationTokenSource != null)
                {
                    ConnectionCancellationTokenSource.Cancel();
                    ConnectionCancellationTokenSource.Dispose();
                }
                ConnectionCancellationTokenSource = new CancellationTokenSource();
            }
        }
        public void MarkInactive()
        {
            IsEnable = false;
            ResetActiveConnections();
        }

        public void MarkActive()
        {
            IsEnable = true;
        }
    }
}