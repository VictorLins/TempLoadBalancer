using Serilog;
using System.Net.Sockets;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Networking
{
    public class ConnectionHandler
    {
        private readonly TcpClient _client;
        private readonly BackendStatus _backend;
        private readonly CancellationToken _cancellationToken;

        public ConnectionHandler(TcpClient prTcpClient, BackendStatus prBackendStatus, CancellationToken prCancellationToken)
        {
            _client = prTcpClient;
            _backend = prBackendStatus;
            _cancellationToken = prCancellationToken;
        }

        public async Task HandleAsync()
        {
            try
            {
                using TcpClient lBackendClient = new TcpClient();
                await lBackendClient.ConnectAsync(_backend.Endpoint.Host, _backend.Endpoint.Port, _cancellationToken);
                Log.Information($"Connected to backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");

                _backend.IncrementActiveConnections();

                using NetworkStream lClientStream = _client.GetStream();
                using NetworkStream lBackendStream = lBackendClient.GetStream();

                using CancellationTokenSource lCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);

                Task lForwardClientToBackend = Task.Run(async () =>
                {
                    try
                    {
                        await lClientStream.CopyToAsync(lBackendStream, lCancellationTokenSource.Token);
                    }
                    catch { /* Do Nothing */}
                }, lCancellationTokenSource.Token);

                Task lForwardBackendToClient = Task.Run(async () =>
                {
                    try
                    {
                        await lBackendStream.CopyToAsync(lClientStream, lCancellationTokenSource.Token);
                    }
                    catch { /* Do Nothing */}
                }, lCancellationTokenSource.Token);

                await Task.WhenAny(lForwardClientToBackend, lForwardBackendToClient);
                lCancellationTokenSource.Cancel();
                await Task.WhenAll(lForwardClientToBackend, lForwardBackendToClient);
            }
            catch (OperationCanceledException)
            {
                Log.Information("Connection handling canceled.");
            }
            catch (Exception prException)
            {
                Log.Warning(prException, $"Connection handling failed for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
            }
            finally
            {
                _client.Close();
                _backend.DecrementActiveConnections();
                Log.Information($"Connection closed for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
            }
        }
    }
}