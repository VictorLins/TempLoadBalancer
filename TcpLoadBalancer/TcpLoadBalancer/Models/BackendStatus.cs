namespace TcpLoadBalancer.Models
{
    public class BackendStatus
    {
        public BackendEndpoint Endpoint { get; init; } = null!;
        public bool IsHealthy { get; set; } = true;
        private int _activeConnections;
        public int ActiveConnections => _activeConnections;

        public void IncrementActiveConnections() => Interlocked.Increment(ref _activeConnections);
        public void DecrementActiveConnections() => Interlocked.Decrement(ref _activeConnections);
    }
}