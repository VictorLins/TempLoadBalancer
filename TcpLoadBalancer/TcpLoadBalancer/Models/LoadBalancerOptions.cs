namespace TcpLoadBalancer.Models
{
    public class LoadBalancerOptions
    {
        public string Strategy { get; set; } = "RoundRobin";
        public string ListenEndpoint { get; set; } = "0.0.0.0:9000";
        public List<BackendEndpoint> Backends { get; set; } = new();
        public int HealthCheckIntervalSeconds { get; set; } = 10;
        public int DefaultIdleTimeoutSeconds { get; set; } = 600; // 10 minutes
        public string StatusFilePath { get; set; } = "status/loadbalancer-status.json";
    }
}