using Serilog;
using System.Net.Sockets;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Health
{
    public class HealthCheckService
    {
        private readonly List<BackendStatus> _backends;
        private readonly int _intervalSeconds;
        private readonly CancellationToken _cancellationToken;

        public HealthCheckService(List<BackendStatus> prBackends, int prIntervalSeconds, CancellationToken prCancellationToken)
        {
            _backends = prBackends;
            _intervalSeconds = prIntervalSeconds;
            _cancellationToken = prCancellationToken;
        }

        public async Task StartAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                foreach (BackendStatus lBackendStatusCurrent in _backends)
                {
                    bool lWasHealthy = lBackendStatusCurrent.IsHealthy;
                    bool lIsHealthyNow = await CheckBackendAsync(lBackendStatusCurrent);

                    if (lWasHealthy != lIsHealthyNow)
                    {
                        lBackendStatusCurrent.IsHealthy = lIsHealthyNow;
                        Log.Information($"Backend {lBackendStatusCurrent.Endpoint.Host}:{lBackendStatusCurrent.Endpoint.Port} health changed: " + (lIsHealthyNow ? "Healthy" : "Unhealthy"));

                        if (!lIsHealthyNow)
                        {
                            lBackendStatusCurrent.MarkInactive();
                            Log.Information($"Backend {lBackendStatusCurrent.Endpoint.Host}:{lBackendStatusCurrent.Endpoint.Port} marked inactive, connections reset.");
                        }

                        if (lIsHealthyNow)
                        {
                            if (!_backends.Contains(lBackendStatusCurrent))
                            {
                                _backends.Add(lBackendStatusCurrent);
                                Log.Information($"Backend {lBackendStatusCurrent.Endpoint.Host}:{lBackendStatusCurrent.Endpoint.Port} recovered and added to status list.");
                            }
                            else if (!lBackendStatusCurrent.IsEnable)
                            {
                                lBackendStatusCurrent.MarkActive();
                                Log.Information($"Backend {lBackendStatusCurrent.Endpoint.Host}:{lBackendStatusCurrent.Endpoint.Port} recovered and enabled.");
                            }
                        }
                    }
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), _cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Exit gracefully
                }
            }
            Log.Information("HealthCheckService stopped.");
        }

        protected virtual async Task<bool> CheckBackendAsync(BackendStatus prBackendStatus)
        {
            try
            {
                using TcpClient lTcpClient = new TcpClient();
                var lConnectTask = lTcpClient.ConnectAsync(prBackendStatus.Endpoint.Host, prBackendStatus.Endpoint.Port);
                var lTtimeoutTask = Task.Delay(3000, _cancellationToken);

                var lCompleted = await Task.WhenAny(lConnectTask, lTtimeoutTask);
                return lCompleted == lConnectTask && lTcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }

        public void UpdateBackends(List<BackendStatus> prBackends)
        {
            _backends.Clear();
            _backends.AddRange(prBackends);
            Log.Information("HealthCheckService backends updated.");
        }
    }
}