using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Backends
{
    public class LeastConnectionsBackendSelector : IBackendSelector
    {
        private List<BackendStatus> _backends;
        private readonly object _lock = new object();

        public LeastConnectionsBackendSelector(List<BackendStatus> prBackends)
        {
            _backends = prBackends;
        }

        public BackendStatus GetNextBackend()
        {
            List<BackendStatus> lHealthyBackends = _backends.Where(b => b.IsHealthy && b.IsEnable).ToList();
            if (!lHealthyBackends.Any())
                throw new InvalidOperationException("No healthy backends available.");

            return lHealthyBackends.OrderBy(b => b.ActiveConnections).First();
        }

        public void UpdateBackends(List<BackendStatus> prNewBackends)
        {
            lock (_lock)
            {
                _backends = prNewBackends;
            }
        }
    }
}