using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Backends
{
    public class RandomBackendSelector : IBackendSelector
    {
        private readonly List<BackendStatus> _backends;
        private Random _Random = new Random();

        public RandomBackendSelector(List<BackendStatus> backends)
        {
            _backends = backends;
        }

        public BackendStatus GetNextBackend()
        {
            List<BackendStatus> lHealhtyBackends = _backends.Where(b => b.IsHealthy).ToList();

            if (!lHealhtyBackends.Any())
                throw new InvalidOperationException("No healthy backends available.");

            int index = _Random.Next(lHealhtyBackends.Count);
            return lHealhtyBackends[index];
        }
    }
}