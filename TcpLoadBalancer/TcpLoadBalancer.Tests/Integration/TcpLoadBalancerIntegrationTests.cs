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
    [Collection("IntegrationTests")]
    public class TcpLoadBalancerIntegrationTests
    {
        /// <summary>
        /// Integration tests for the TCP load balancer.
        /// 
        /// These tests verify that:
        /// - Data sent by a TCP client to the load balancer is correctly forwarded to healthy backend servers.
        /// - Backends receive the expected messages.
        /// - The load balancer behaves correctly under different scenarios (e.g., healthy/unhealthy backends, multiple clients).
        /// 
        /// Ports used in these tests are fixed for isolation, but care should be taken if running tests in parallel.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public async Task ClientData_IsForwarded_ToHealthyBackend()
        {
            // Arrange
            using var lBackend = new TestTcpServer(9000);

            var lBackendStatus = new BackendStatus
            {
                Endpoint = new BackendEndpoint
                {
                    Host = "127.0.0.1",
                    Port = 9000
                },
                IsHealthy = true
            };

            var lBackendSelector = new RandomBackendSelector(new List<BackendStatus> { lBackendStatus });

            using var lCancellationTokenSource = new CancellationTokenSource();

            var lListener = new TcpListenerService(
                () => lBackendSelector,
                new IPEndPoint(IPAddress.Loopback, 9001),
                lCancellationTokenSource.Token,
                LoadBalancerOptionsHelper.GetOptionsMonitorFake());

            var lListenerTask = lListener.StartAsync();

            // Give listener time to start
            await Task.Delay(500);

            // Act
            using var lClient = new TcpClient();
            await lClient.ConnectAsync("127.0.0.1", 9001);

            using var lStream = lClient.GetStream();
            var lMessage = "hello-backend";
            var lBytes = Encoding.UTF8.GetBytes(lMessage + "\n");

            await lStream.WriteAsync(lBytes);
            await lStream.FlushAsync();
            await Task.Delay(500);

            // Assert
            Assert.Contains("hello-backend", lBackend.ReceivedMessages);

            // Cleanup
            lCancellationTokenSource.Cancel();
            await lListenerTask;
        }
    }
}