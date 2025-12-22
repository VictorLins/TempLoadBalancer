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

        protected virtual TcpClient CreateBackendTcpClient() => new TcpClient();

        public async Task HandleAsync()
        {
            bool lIncremented = false;
            try
            {
                using TcpClient lBackendClient = CreateBackendTcpClient();
                await lBackendClient.ConnectAsync(_backend.Endpoint.Host, _backend.Endpoint.Port, _cancellationToken);
                Log.Information($"Connected to backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");

                _backend.IncrementActiveConnections();
                lIncremented = true;

                using NetworkStream lClientStream = _client.GetStream();
                using NetworkStream lBackendStream = lBackendClient.GetStream();

                using CancellationTokenSource lLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);

                Task lForwardClientToBackend = Task.Run(async () =>
                {
                    try
                    {
                        await lClientStream.CopyToAsync(lBackendStream, lLinkedCancellationTokenSource.Token);
                    }
                    catch { /* Do Nothing */}
                }, lLinkedCancellationTokenSource.Token);


                Task lForwardBackendToClient = Task.Run(async () =>
                {
                    try
                    {
                        await lBackendStream.CopyToAsync(lClientStream, lLinkedCancellationTokenSource.Token);
                    }
                    catch { /* Do Nothing */}
                }, lLinkedCancellationTokenSource.Token);

                await Task.WhenAny(lForwardClientToBackend, lForwardBackendToClient);
                lLinkedCancellationTokenSource.Cancel();
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
                if (lIncremented)
                    _backend.DecrementActiveConnections();
                Log.Information($"Connection closed for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
            }
        }
    }
}