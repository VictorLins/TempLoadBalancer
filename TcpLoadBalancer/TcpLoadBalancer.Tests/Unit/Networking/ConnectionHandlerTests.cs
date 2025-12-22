using System.Net;
using System.Net.Sockets;
using System.Text;
using TcpLoadBalancer.Models;
using TcpLoadBalancer.Networking;
using TcpLoadBalancer.Tests.TestHelpers;

namespace TcpLoadBalancer.Tests.Unit.Networking
{
    public class ConnectionHandlerTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public async Task HandleAsync_BackendConnectionSuccessful_IncrementsAndDecrementsActiveConnections()
        {
            // Arrange
            using var lBackendServer = new TestTcpServer(6000);
            var lBackend = new BackendStatus
            {
                Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = 6000 },
                IsHealthy = true
            };

            using var lClient = new TcpClient();
            await lClient.ConnectAsync("127.0.0.1", 6000);

            var lHandler = new ConnectionHandler(lClient, lBackend, CancellationToken.None);

            // Act: write a small message to unblock CopyToAsync
            var clientStream = lClient.GetStream();
            byte[] dummyMessage = Encoding.UTF8.GetBytes("X\n");
            await clientStream.WriteAsync(dummyMessage);
            await clientStream.FlushAsync();

            // Wait a short time to allow copy to complete
            await Task.Delay(100);

            await lHandler.HandleAsync();

            // Assert
            Assert.Equal(0, lBackend.ActiveConnections);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task HandleAsync_Cancellation_DoesNotThrowAndDecrementsActiveConnections()
        {
            // Arrange
            int lPort = 6001;
            BackendStatus lBackend = new BackendStatus
            {
                Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = lPort },
                IsHealthy = true
            };

            using TcpListener lListener = new TcpListener(IPAddress.Loopback, lPort);
            lListener.Start();

            using TcpClient lClient = new TcpClient();
            CancellationTokenSource lCts = new CancellationTokenSource();
            lCts.Cancel(); // cancel immediately

            await lClient.ConnectAsync("127.0.0.1", lPort);

            var lHandler = new ConnectionHandler(lClient, lBackend, lCts.Token);

            // Act
            await lHandler.HandleAsync();

            // Assert
            Assert.Equal(0, lBackend.ActiveConnections);
        }
    }
}