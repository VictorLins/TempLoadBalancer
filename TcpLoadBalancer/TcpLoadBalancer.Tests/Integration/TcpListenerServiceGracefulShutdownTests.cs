using System.Net;
using System.Net.Sockets;
using System.Text;
using TcpLoadBalancer.Backends;
using TcpLoadBalancer.Models;
using TcpLoadBalancer.Networking;
using TcpLoadBalancer.Tests.TestHelpers;

namespace TcpLoadBalancer.Tests.Integration
{
    /// <summary>
    /// Integration tests for the TcpListenerService to verify graceful shutdown behavior.
    /// 
    /// These tests ensure that the listener stops accepting new client connections
    /// when the cancellation token is triggered, while allowing in-flight connections to complete.
    /// </summary>
    [Collection("TcpIntegrationTests")]
    public class TcpListenerServiceGracefulShutdownTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task TcpListenerService_StopsAcceptingConnections_OnCancellation()
        {
            // Arrange
            using var lBackendServer = new TestTcpServer(9006); // Test backend
            var lBackendStatus = new BackendStatus
            {
                Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = 9006 },
                IsHealthy = true
            };

            var lBackendSelector = new RandomBackendSelector(new List<BackendStatus> { lBackendStatus });

            using var lCancellationTokenSource = new CancellationTokenSource();

            var lListener = new TcpListenerService(
                lBackendSelector,
                new IPEndPoint(IPAddress.Loopback, 9007), // listener port
                lCancellationTokenSource.Token);

            var lListenerTask = lListener.StartAsync();
            await Task.Delay(500);

            // Act: Connect a client before shutdown
            using var lClient1 = new TcpClient();
            await lClient1.ConnectAsync("127.0.0.1", 9007);

            var lClient1Stream = lClient1.GetStream();
            var lMessage1 = "before-shutdown";
            await lClient1Stream.WriteAsync(Encoding.UTF8.GetBytes(lMessage1 + "\n"));
            await lClient1Stream.FlushAsync();
            await Task.Delay(500);

            lCancellationTokenSource.Cancel();

            // Try connecting a second client after shutdown
            var lClient2 = new TcpClient();
            var lConnectExceptionThrown = false;
            try
            {
                await lClient2.ConnectAsync("127.0.0.1", 9007);
            }
            catch (Exception)
            {
                lConnectExceptionThrown = true;
            }

            // Wait for listener task to finish
            await lListenerTask;

            // Assert
            // First client should have reached backend
            Assert.Contains("before-shutdown", lBackendServer.ReceivedMessages);
            // Second client should NOT connect
            Assert.True(lConnectExceptionThrown, "Listener should not accept new connections after cancellation");
        }
    }
}