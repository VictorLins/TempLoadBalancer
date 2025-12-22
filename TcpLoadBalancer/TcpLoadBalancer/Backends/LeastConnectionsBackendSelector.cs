using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Backends
{
    public class LeastConnectionsBackendSelector : IBackendSelector
    {
        private readonly List<BackendStatus> _backends;

        public LeastConnectionsBackendSelector(List<BackendStatus> backends)
        {
            _backends = backends;
        }

        public BackendStatus GetNextBackend()
        {
            List<BackendStatus> lHealthyBackends = _backends.Where(b => b.IsHealthy).ToList();
            if (!lHealthyBackends.Any())
                throw new InvalidOperationException("No healthy backends available.");

            return lHealthyBackends.OrderBy(b => b.ActiveConnections).First();
        }
    }
}