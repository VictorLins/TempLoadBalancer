using Microsoft.Extensions.Configuration;
using Serilog;
using System.Net;
using System.Threading.Tasks;
using TcpLoadBalancer.Backends;
using TcpLoadBalancer.Health;
using TcpLoadBalancer.Models;
using TcpLoadBalancer.Networking;
using TcpLoadBalancer.Status;


namespace TcpLoadBalancer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Load configuration from appsettings.json
            var lConfiguration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // 2. Configure Serilog logging
            Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(lConfiguration)
            .Enrich.FromLogContext()
            .CreateLogger();

            // 3. Set up cancellation
            CancellationTokenSource lCancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true; // prevent immediate termination
                Log.Information("Cancellation requested, shutting down...");
                lCancellationTokenSource.Cancel();
            };

            // 4. Read load balancer configuration
            var lLoadBalancerConfig = lConfiguration.GetSection("LoadBalancer");
            var lListenEndpointsParts = lLoadBalancerConfig["ListenEndpoint"]!.Split(':');
            var lListenEndoints = new IPEndPoint(IPAddress.Parse(lListenEndpointsParts[0]), int.Parse(lListenEndpointsParts[1]));

            var lBackends = lLoadBalancerConfig.GetSection("Backends").Get<List<BackendEndpoint>>()!
                .Select(b => new BackendStatus { Endpoint = b })
                .ToList();

            // 5. Create backend selector (round-robin)
            IBackendSelector lBackendSelector = new RoundRobinBackendSelector(lBackends);

            // 6. Initialize services
            var lListenerService = new TcpListenerService(lBackendSelector, lListenEndoints, lCancellationTokenSource.Token);
            var lHealthService = new HealthCheckService(lBackends, int.Parse(lLoadBalancerConfig["HealthCheckIntervalSeconds"] ?? "10"), lCancellationTokenSource.Token);
            var lStatusWriter = new StatusFileWriter(lBackends, lLoadBalancerConfig["StatusFilePath"]!, 5, lCancellationTokenSource.Token);

            // 7. Run all services concurrently
            var lTasks = new Task[]
            {
                lListenerService.StartAsync(),
                lHealthService.StartAsync(),
                lStatusWriter.StartAsync()
            };

            // 8. Start
            Log.Information("TcpLoadBalancer starting...");
            await Task.WhenAll(lTasks);
            Log.Information("TcpLoadBalancer stopped.");
            Log.CloseAndFlush();

            Console.WriteLine("Hello, World!");
        }
    }
}