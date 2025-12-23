using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Backends
{
    public interface IBackendSelector
    {
        BackendStatus GetNextBackend();
        void UpdateBackends(List<BackendStatus> prNewBackends);
    }
}