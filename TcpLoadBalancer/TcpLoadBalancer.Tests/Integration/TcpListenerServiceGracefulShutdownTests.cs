using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TcpLoadBalancer.Backends;
using TcpLoadBalancer.Models;
using TcpLoadBalancer.Networking;
using TcpLoadBalancer.Tests.TestHelpers;
using TcpLoadBalancer.Tests.Unit.Models;

namespace TcpLoadBalancer.Tests.Integration
{
    /// <summary>
    /// Integration tests for the TcpListenerService to verify graceful shutdown behavior.
    /// 
    /// These tests ensure that the listener stops accepting new client connections
    /// when the cancellation token is triggered, while allowing in-flight connections to complete.
    /// </summary>
    [Collection("IntegrationTests")]
    public class TcpListenerServiceGracefulShutdownTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task TcpListenerService_StopsAcceptingConnections_OnCancellation()
        {
            // Arrange
            int lBackendPort = 9006;
            int lListenerPort = 9007;

            using var lBackendServer = new TestTcpServer(lBackendPort);
            var lBackendStatus = new BackendStatus
            {
                Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = lBackendPort },
                IsHealthy = true
            };

            var lBackendSelector = new RandomBackendSelector(new List<BackendStatus> { lBackendStatus });
            using var lCancellationTokenSource = new CancellationTokenSource();

            var lListener = new TcpListenerService(
                () => lBackendSelector,
                new IPEndPoint(IPAddress.Loopback, lListenerPort),
                lCancellationTokenSource.Token,
                LoadBalancerOptionsHelper.GetOptionsMonitorFake());

            var lListenerTask =  lListener.StartAsync();
            await Task.Delay(500);

            // Act
            using (var lClient1 = new TcpClient())
            {
                await lClient1.ConnectAsync("127.0.0.1", lListenerPort);
                using var stream = lClient1.GetStream();
                var data = Encoding.UTF8.GetBytes("before-shutdown\n");
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();

                await Task.Delay(500);
            }

            lCancellationTokenSource.Cancel();
            await Task.Delay(500);

            bool lConnectExceptionThrown = false;
            try
            {
                using var lClient2 = new TcpClient();
                using var lCancellationTokenSource2 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await lClient2.ConnectAsync("127.0.0.1", lListenerPort, lCancellationTokenSource2.Token);

                lClient2.GetStream().Write(new byte[] { 0 }, 0, 1);
                lConnectExceptionThrown = false;
            }
            catch { lConnectExceptionThrown = true; }

            await Task.WhenAny(lListenerTask, Task.Delay(1000));

            // Assert
            var start = DateTime.Now;
            while (!lBackendServer.ReceivedMessages.Any() && (DateTime.Now - start).TotalSeconds < 2)
            {
                await Task.Delay(100);
            }

            Assert.NotEmpty(lBackendServer.ReceivedMessages);
            Assert.Contains(lBackendServer.ReceivedMessages, m => m.Contains("before-shutdown"));
            Assert.True(lConnectExceptionThrown, "O listener aceitou conexão após cancelamento");
        }
    }
}