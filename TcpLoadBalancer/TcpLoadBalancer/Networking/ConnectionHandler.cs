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

                _externalClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                lInternalClientToTheBackend.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                _externalClient.Client.NoDelay = true;
                lInternalClientToTheBackend.Client.NoDelay = true;

                SetTcpKeepAlive(_externalClient.Client, 30, 10, 3);
                SetTcpKeepAlive(lInternalClientToTheBackend.Client, 30, 10, 3);

                _externalClient.ReceiveTimeout = 0;
                _externalClient.SendTimeout = 0;
                lInternalClientToTheBackend.ReceiveTimeout = 0;
                lInternalClientToTheBackend.SendTimeout = 0;

                Log.Information($"Connected to backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");

                _backend.IncrementActiveConnections();
                lIncremented = true;

                using NetworkStream lClientStream = _externalClient.GetStream();
                using NetworkStream lBackendStream = lInternalClientToTheBackend.GetStream();

                lClientStream.ReadTimeout = Timeout.Infinite;
                lClientStream.WriteTimeout = Timeout.Infinite;
                lBackendStream.ReadTimeout = Timeout.Infinite;
                lBackendStream.WriteTimeout = Timeout.Infinite;

                var lBuffer = new byte[8192];

                // Shared cancellation to stop both directions when one fails
                using var lConnectionCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken,
                    _backend.ConnectionCancellationTokenSource?.Token ?? _cancellationToken);

                async Task CopyStream(NetworkStream prFrom, NetworkStream prTo, string direction)
                {
                    int iterations = 0;
                    try
                    {
                        while (!lConnectionCts.Token.IsCancellationRequested)
                        {
                            int lRead = await prFrom.ReadAsync(lBuffer, 0, lBuffer.Length, lConnectionCts.Token);

                            if (lRead == 0)
                            {
                                Log.Information($"[{direction}] Connection closed gracefully (0 bytes read)");
                                break;
                            }

                            await prTo.WriteAsync(lBuffer, 0, lRead, lConnectionCts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Log.Information($"[{direction}] Operation canceled");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, $"[{direction}] Stream error");
                    }
                    finally
                    {
                        // Cancel the other direction when one direction completes
                        lConnectionCts.Cancel();
                        Log.Information($"[{direction}] Stream copy completed");
                    }
                }

                Task lForwardClientToBackend = CopyStream(lClientStream, lBackendStream, "Client->Backend");
                Task lForwardBackendToClient = CopyStream(lBackendStream, lClientStream, "Backend->Client");

                Log.Information($"Starting bidirectional forwarding for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
                await Task.WhenAll(lForwardClientToBackend, lForwardBackendToClient);
                Log.Information($"Bidirectional forwarding completed for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
            }
            catch (IOException prIOException) when (prIOException.InnerException is SocketException se)
            {
                Log.Information($"Socket error {se.SocketErrorCode}: {se.Message} for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
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
                _externalClient.Close();
                if (lIncremented)
                    _backend.DecrementActiveConnections();
                Log.Information($"Connection closed for backend {_backend.Endpoint.Host}:{_backend.Endpoint.Port}");
            }
        }

        private void SetTcpKeepAlive(Socket prSocket, int prKeepAliveTime, int prKeepAliveInterval, int prKeepAliveRetryCount)
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    prSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, prKeepAliveTime);
                    prSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, prKeepAliveInterval);
                    prSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, prKeepAliveRetryCount);
                    Log.Debug($"TCP Keep-Alive configured: Time={prKeepAliveTime}s, Interval={prKeepAliveInterval}s, Retries={prKeepAliveRetryCount}");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to set TCP Keep-Alive parameters");
            }
        }
    }
}