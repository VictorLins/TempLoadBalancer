using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Backends
{
    public class RandomBackendSelector : IBackendSelector
    {
        private List<BackendStatus> _backends;
        private Random _random = new Random();
        private readonly object _lock = new object();

        public RandomBackendSelector(List<BackendStatus> prBackends)
        {
            _backends = prBackends;
        }

        public BackendStatus GetNextBackend()
        {
            List<BackendStatus> lHealthyBackends = _backends.Where(b => b.IsHealthy && b.IsEnable).ToList();

            if (!lHealthyBackends.Any())
                throw new InvalidOperationException("No healthy backends available.");

            int lIndex = _random.Next(lHealthyBackends.Count);
            return lHealthyBackends[lIndex];
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