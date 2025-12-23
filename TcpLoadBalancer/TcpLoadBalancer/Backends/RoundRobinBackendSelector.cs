using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Backends
{
    public class RoundRobinBackendSelector : IBackendSelector
    {
        private List<BackendStatus> _backends;
        private int _index = -1;
        private readonly object _lock = new object();

        public RoundRobinBackendSelector(List<BackendStatus> prBackends)
        {
            _backends = prBackends;
        }

        public BackendStatus GetNextBackend()
        {
            for (int lIndex = 0; lIndex < _backends.Count; lIndex++)
            {
                int lNextIndex = Interlocked.Increment(ref _index) % _backends.Count;
                BackendStatus lBackendStatus = _backends[lNextIndex];

                if (lBackendStatus.IsHealthy && lBackendStatus.IsEnable)
                    return lBackendStatus;
            }

            throw new InvalidOperationException("No healthy backends available.");
        }

        public void UpdateBackends(List<BackendStatus> prNewBackends)
        {
            lock (_lock)
            {
                _backends = prNewBackends;
                if (_index >= _backends.Count)
                    _index = -1;
            }
        }
    }
}