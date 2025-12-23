using Serilog;
using System.Text.Json;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Status
{
    public class StatusFileWriter
    {
        private readonly List<BackendStatus> _backends;
        private readonly string _filePath;
        private readonly int _intervalSeconds;
        private readonly CancellationToken _cancellationToken;
        private readonly object _fileLock = new object();

        public StatusFileWriter(List<BackendStatus> prBackends, string prFilePath, int prIntervalSeconds, CancellationToken prCancellationToken)
        {
            _backends = prBackends;
            _filePath = prFilePath;
            _intervalSeconds = prIntervalSeconds;
            _cancellationToken = prCancellationToken;
        }

        public async Task StartAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var lStatus = new
                    {
                        timestampUtc = DateTime.UtcNow,
                        activeConnections = _backends.Sum(b => b.ActiveConnections),
                        backends = _backends.Select(b => new
                        {
                            endpoint = $"{b.Endpoint.Host}:{b.Endpoint.Port}",
                            healthy = b.IsHealthy,
                            enabled = b.IsEnable,
                            activeConnections = b.ActiveConnections
                        }).ToList()
                    };

                    var lJson = JsonSerializer.Serialize(lStatus, new JsonSerializerOptions { WriteIndented = true });
                    var lDirectory = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrEmpty(lDirectory))
                        Directory.CreateDirectory(lDirectory);
                    lock (_fileLock)
                    {
                        File.WriteAllText(_filePath, lJson);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Exit gracefully
                }
                catch (Exception prException)
                {
                    Log.Warning(prException, "Failed to write status file");
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
        }
    }
}