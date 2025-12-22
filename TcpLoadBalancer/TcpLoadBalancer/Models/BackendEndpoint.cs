namespace TcpLoadBalancer.Models
{
    public class BackendEndpoint
    {
        public string Host { get; init; } = "";
        public int Port { get; init; }
    }
}