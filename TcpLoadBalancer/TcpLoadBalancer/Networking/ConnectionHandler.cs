using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Net.Sockets;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Networking
{
    public class ConnectionHandler
    {
        private readonly TcpClient _externalClient;
        private readonly BackendStatus _backend;
        private readonly CancellationToken _cancellationToken;
        private readonly IOptionsMonitor<LoadBalancerOptions> _loadBalancerOptions;

        public ConnectionHandler(TcpClient prTcpClient,
            BackendStatus prBackendStatus,
            CancellationToken prCancellationToken,
            IOptionsMonitor<LoadBalancerOptions> prLoadBalancerOptions)
        {
            _externalClient = prTcpClient;
            _backend = prBackendStatus;
            _cancellationToken = prCancellationToken;
            _loadBalancerOptions = prLoadBalancerOptions;
        }

        protected virtual TcpClient CreateBackendTcpClient() => new TcpClient();

        public async Task HandleAsync()
        {
            bool lIncremented = false;
            try
            {
                using TcpClient lInternalClientToTheBackend = CreateBackendTcpClient();
                await lInternalClientToTheBackend.ConnectAsync(_backend.Endpoint.Host, _backend.Endpoint.Port, _cancellationToken);

                // Set socket options BEFORE getting streams
                _externalClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                lInternalClientToTheBackend.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                // Add these timeout settings
                _externalClient.ReceiveTimeout = 0; // Disable receive timeout
                _externalClient.SendTimeout = 0; // Disable send timeout
                lInternalClientToTheBackend.ReceiveTimeout = 0;
                lInternalClientToTheBackend.SendTimeout = 0;

                Log.Information($"Connected to backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");

                _backend.IncrementActiveConnections();
                lIncremented = true;

                using NetworkStream lClientStream = _externalClient.GetStream();
                using NetworkStream lBackendStream = lInternalClientToTheBackend.GetStream();

                // Add read/write timeouts to streams as well
                lClientStream.ReadTimeout = Timeout.Infinite;
                lClientStream.WriteTimeout = Timeout.Infinite;
                lBackendStream.ReadTimeout = Timeout.Infinite;
                lBackendStream.WriteTimeout = Timeout.Infinite;

                int lIdleTimeoutSeconds = _loadBalancerOptions.CurrentValue.DefaultIdleTimeoutSeconds;
                Log.Information($"Using idle timeout of {lIdleTimeoutSeconds} seconds");

                var lBuffer = new byte[8192];

                async Task CopyStreamWithIdleTimeout(NetworkStream prFrom, NetworkStream prTo, string direction, CancellationToken prCancellationToken)
                {
                    int iterations = 0;
                    while (!prCancellationToken.IsCancellationRequested)
                    {
                        int lRead;
                        try
                        {
                            Log.Debug($"[{direction}] Waiting for data... (iteration {++iterations})");

                            var lReadTask = prFrom.ReadAsync(lBuffer, 0, lBuffer.Length, prCancellationToken);
                            var lTimeoutTask = Task.Delay(TimeSpan.FromSeconds(lIdleTimeoutSeconds), prCancellationToken);

                            var lCompletedTask = await Task.WhenAny(lReadTask, lTimeoutTask);

                            if (lCompletedTask == lTimeoutTask)
                            {
                                Log.Information($"[{direction}] Idle timeout reached after {lIdleTimeoutSeconds} seconds for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
                                break;
                            }

                            lRead = await lReadTask;
                            Log.Debug($"[{direction}] Read {lRead} bytes");
                        }
                        catch (OperationCanceledException)
                        {
                            Log.Information($"[{direction}] Operation canceled for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, $"[{direction}] Read error for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
                            break;
                        }

                        if (lRead == 0)
                        {
                            Log.Information($"[{direction}] Connection closed (0 bytes read) for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
                            break;
                        }

                        await prTo.WriteAsync(lBuffer, 0, lRead, prCancellationToken);
                    }

                    Log.Information($"[{direction}] Stream copy completed");
                }

                var lLinkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
                    _cancellationToken, _backend.ConnectionCancellationTokenSource?.Token ?? _cancellationToken);

                Task lForwardClientToBackend = CopyStreamWithIdleTimeout(lClientStream, lBackendStream, "Client->Backend", lLinkedCancellationToken.Token);
                Task lForwardBackendToClient = CopyStreamWithIdleTimeout(lBackendStream, lClientStream, "Backend->Client", lLinkedCancellationToken.Token);

                Log.Information($"Starting bidirectional forwarding for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
                await Task.WhenAll(lForwardClientToBackend, lForwardBackendToClient);
                Log.Information($"Bidirectional forwarding completed for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
            }
            catch (IOException prIOException) when (prIOException.InnerException is SocketException se)
            {
                Log.Information($"Socket error {se.SocketErrorCode}: {se.Message} for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
            }
            catch (OperationCanceledException prOperationCanceledException)
            {
                Log.Information("Connection handling canceled.");
            }
            catch (Exception prException)
            {
                Log.Warning(prException, $"Connection handling failed for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
            }
            finally
            {
                _externalClient.Close();
                if (lIncremented)
                    _backend.DecrementActiveConnections();
                Log.Information($"Connection closed for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
            }
        }
    }
}