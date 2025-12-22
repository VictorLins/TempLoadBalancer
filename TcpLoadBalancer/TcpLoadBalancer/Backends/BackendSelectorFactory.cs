using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Backends
{
    public class BackendSelectorFactory
    {
        public static IBackendSelector CreateBackendSelector(string prStrategy, List<BackendStatus> prBackends)
        {
            return prStrategy switch
            {
                "RoundRobin" => new RoundRobinBackendSelector(prBackends),
                "Random" => new RandomBackendSelector(prBackends),
                "LeastConnections" => new LeastConnectionsBackendSelector(prBackends),
                _ => throw new ArgumentException($"Unknown backend selection strategy: {prStrategy}")
            };
        }
    }
}