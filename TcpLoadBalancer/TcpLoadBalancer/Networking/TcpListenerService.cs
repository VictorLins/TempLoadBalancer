using Serilog;
using System.Net;
using System.Net.Sockets;
using TcpLoadBalancer.Backends;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Networking
{
    public class TcpListenerService
    {
        private readonly IBackendSelector _backendSelector;
        private readonly IPEndPoint _listenEndpoint;
        private readonly CancellationToken _cancellationToken;

        public TcpListenerService(IBackendSelector prBackendSelector, IPEndPoint ptListenEndpoint, CancellationToken prCancellationToken)
        {
            _backendSelector = prBackendSelector;
            _listenEndpoint = ptListenEndpoint;
            _cancellationToken = prCancellationToken;
        }

        public async Task StartAsync()
        {
            using TcpListener lTcpListener = new TcpListener(_listenEndpoint);
            lTcpListener.Start();
            Log.Information($"TCP Listener started on {_listenEndpoint.Address}:{_listenEndpoint.Port}");

            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    // Accept incoming client
                    TcpClient lTcpClient = await lTcpListener.AcceptTcpClientAsync(_cancellationToken);
                    Log.Information($"Accepted connection from {lTcpClient.Client.RemoteEndPoint}");

                    // Select a backend
                    BackendStatus lBackendStatus;
                    try
                    {
                        lBackendStatus = _backendSelector.GetNextBackend();
                    }
                    catch (InvalidOperationException)
                    {
                        Log.Warning("No healthy backends available. Closing connection.");
                        lTcpClient.Close();
                        continue;
                    }

                    // Handle connection in a separate task
                    _ = Task.Run(async () =>
                    {
                        ConnectionHandler lConnectionHandler = new ConnectionHandler(lTcpClient, lBackendStatus, _cancellationToken);
                        await lConnectionHandler.HandleAsync();
                    }, _cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Information("TCP Listener is shutting down due to cancellation.");
            }
            finally
            {
                lTcpListener.Stop();
                Log.Information("TCP Listener stopped.");
            }
        }
    }
}