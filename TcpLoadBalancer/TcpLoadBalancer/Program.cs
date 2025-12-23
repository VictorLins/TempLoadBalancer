using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Serilog;
using System.Net;
using TcpLoadBalancer.Backends;
using TcpLoadBalancer.Health;
using TcpLoadBalancer.Models;
using TcpLoadBalancer.Networking;
using TcpLoadBalancer.Status;


namespace TcpLoadBalancer
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            await RunProcesses();
        }

        public static async Task RunProcesses()
        {
            // 1. Appsettings
            var lConfiguration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // 2. Serilog
            Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(lConfiguration)
            .Enrich.FromLogContext()
            .CreateLogger();

            // 3. DI
            var lServices = new ServiceCollection();

            lServices.Configure<LoadBalancerOptions>(
                lConfiguration.GetSection("LoadBalancer"));

            var lBackendStatuses = new List<BackendStatus>();
            lServices.AddSingleton(lBackendStatuses);

            var lServiceProvider = lServices.BuildServiceProvider();
            var lIOptionsMonitor = lServiceProvider.GetRequiredService<IOptionsMonitor<LoadBalancerOptions>>();

            IBackendSelector lBackendSelector =
                BackendSelectorFactory.CreateBackendSelector(
                    lIOptionsMonitor.CurrentValue.Strategy,
                    lBackendStatuses
                );

            // 4. Sync backends helper
            void SyncBackends(IEnumerable<BackendEndpoint> prEndpoints)
            {
                var lDesiredEndpoints = prEndpoints
                 .Select(e => $"{e.Host}:{e.Port}")
                 .ToHashSet();

                // Remove backends that no longer exist
                lBackendStatuses.RemoveAll(b =>
                    !lDesiredEndpoints.Contains($"{b.Endpoint.Host}:{b.Endpoint.Port}")
                );

                // Add new backends
                foreach (var lEndpointCurrent in prEndpoints)
                {
                    bool lDoesItExist = lBackendStatuses.Any(b =>
                        b.Endpoint.Host == lEndpointCurrent.Host &&
                        b.Endpoint.Port == lEndpointCurrent.Port
                    );

                    if (!lDoesItExist)
                    {
                        lBackendStatuses.Add(new BackendStatus
                        {
                            Endpoint = lEndpointCurrent,
                            IsHealthy = true,
                            IsEnable = true
                        });

                        Log.Information(
                            "New backend added: {Host}:{Port}",
                            lEndpointCurrent.Host,
                            lEndpointCurrent.Port
                        );
                    }
                }

                // Removed backends
                foreach (var b in lBackendStatuses)
                {
                    if (!lDesiredEndpoints.Contains($"{b.Endpoint.Host}:{b.Endpoint.Port}"))
                    {
                        b.IsEnable = false;
                        Log.Information("Backend marked disabled: {Host}:{Port}", b.Endpoint.Host, b.Endpoint.Port);
                    }
                }

                lBackendSelector.UpdateBackends(lBackendStatuses.Where(b => b.IsEnable).ToList());
                Log.Information("Backends after sync: {Backends}", string.Join(", ", lBackendStatuses.Select(b => $"{b.Endpoint.Port}"))
             );
            }

            // Initial state
            SyncBackends(lIOptionsMonitor.CurrentValue.Backends);

            // 5. Listen endpoint
            var lParts = lIOptionsMonitor.CurrentValue.ListenEndpoint.Split(':');
            var lListenEndpoint = new IPEndPoint(IPAddress.Parse(lParts[0]), int.Parse(lParts[1]));

            // 6. Cancellation
            CancellationTokenSource lCancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true; // prevent immediate termination
                Log.Information("Cancellation requested, shutting down...");
                lCancellationTokenSource.Cancel();
            };

            // 7. Services
            var lTcpListenerService = new TcpListenerService(
                () => lBackendSelector,
                lListenEndpoint,
                lCancellationTokenSource.Token,
                lIOptionsMonitor
            );

            var lHealthCheckService = new HealthCheckService(
                lBackendStatuses,
                lIOptionsMonitor.CurrentValue.HealthCheckIntervalSeconds,
                lCancellationTokenSource.Token
            );

            var lStatusFileWriter = new StatusFileWriter(
                lBackendStatuses,
                lIOptionsMonitor.CurrentValue.StatusFilePath,
                5,
                lCancellationTokenSource.Token
            );

            // 8. Config reload handler
            lIOptionsMonitor.OnChange(newOptions =>
            {
                SyncBackends(newOptions.Backends);
                Log.Information("LoadBalancer config reloaded");
            });

            // 9. Run
            Log.Information("TcpLoadBalancer starting...");
            await Task.WhenAll(
                lTcpListenerService.StartAsync(),
                lHealthCheckService.StartAsync(),
                lStatusFileWriter.StartAsync()
            );
        }
    }
}