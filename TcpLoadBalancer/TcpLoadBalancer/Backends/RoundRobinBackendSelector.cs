using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Backends
{
    public class RoundRobinBackendSelector : IBackendSelector
    {
        private readonly List<BackendStatus> _backends;
        private int _index = -1;

        public RoundRobinBackendSelector(List<BackendStatus> backends)
        {
            _backends = backends;
        }

        public BackendStatus GetNextBackend()
        {
            for (int lIndex = 0; lIndex < _backends.Count; lIndex++)
            {
                int lNextIndex = Interlocked.Increment(ref _index) % _backends.Count;
                BackendStatus lBackendStatus = _backends[lNextIndex];

                if (lBackendStatus.IsHealthy)
                    return lBackendStatus;
            }

            throw new InvalidOperationException("No healthy backends available.");
        }
    }
}
