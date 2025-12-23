using Microsoft.Extensions.Options;
using Serilog;
using System.Net;
using System.Net.Sockets;
using TcpLoadBalancer.Backends;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Networking
{
    public class TcpListenerService
    {
        private readonly Func<IBackendSelector> _backendSelectorAccessor;
        private readonly IPEndPoint _listenEndpoint;
        private readonly CancellationToken _cancellationToken;
        private readonly IOptionsMonitor<LoadBalancerOptions> _loadBalancerOptions;
        private IBackendSelector _backendSelector => _backendSelectorAccessor();

        public TcpListenerService(
            Func<IBackendSelector> prBackendSelectorAccessor,
            IPEndPoint prListenEndpoint,
            CancellationToken prCancellationToken,
            IOptionsMonitor<LoadBalancerOptions> prLoadBalancerOptions)
        {
            _backendSelectorAccessor = prBackendSelectorAccessor;
            _listenEndpoint = prListenEndpoint;
            _cancellationToken = prCancellationToken;
            _loadBalancerOptions = prLoadBalancerOptions;
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
                    TcpClient lExternalTcpClient = await lTcpListener.AcceptTcpClientAsync(_cancellationToken);
                    Log.Information($"Accepted connection from {lExternalTcpClient.Client.RemoteEndPoint}");

                    BackendStatus lBackendStatus;
                    try
                    {
                        lBackendStatus = _backendSelector.GetNextBackend();
                    }
                    catch (InvalidOperationException)
                    {
                        Log.Warning("No healthy backends available. Closing connection.");
                        lExternalTcpClient.Close();
                        continue;
                    }

                    _ = Task.Run(async () =>
                    {
                        ConnectionHandler lConnectionHandler = new ConnectionHandler(lExternalTcpClient, lBackendStatus, _cancellationToken, _loadBalancerOptions);
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