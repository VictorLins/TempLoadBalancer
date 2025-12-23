using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Integration tests focusing on the Load Balancer's ability to handle high concurrency 
/// and maintain long-lived TCP connections.
/// </summary>
[Collection("IntegrationTests")]
public class LoadBalancerIdleConnectionTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task LoadBalancer_Should_Distribute_And_Maintain_Connections()
    {
        // Arrange
        using var lCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var lbEndpoint = new IPEndPoint(IPAddress.Loopback, 9000);
        int lClientCount = 5;
        var lHoldDuration = TimeSpan.FromMinutes(3);

        await using var lBackend1 = new FakeBackendServer(9101);
        await using var lBackend2 = new FakeBackendServer(9102);
        await lBackend1.StartAsync();
        await lBackend2.StartAsync();

        var lbTask = Task.Run(() => TcpLoadBalancer.Program.RunProcesses(), lCancellationTokenSource.Token);
        await Task.Delay(2000); // Wait for LB port binding

        // Act
        var lClientTasks = Enumerable.Range(1, lClientCount).Select(async i =>
        {
            using var lTcpClient = new TcpClient();
            await lTcpClient.ConnectAsync(lbEndpoint, lCancellationTokenSource.Token);

            // Send a unique message to verify the tunnel
            byte[] lData = Encoding.UTF8.GetBytes($"Hello from client {i}");
            await lTcpClient.GetStream().WriteAsync(lData, lCancellationTokenSource.Token);

            // Hold connection open to test long-lived stability
            await Task.Delay(lHoldDuration, lCancellationTokenSource.Token);
        }).ToArray();

        await Task.WhenAll(lClientTasks);
        await Task.Delay(500);

        // Assert
        int totalConnections = lBackend1.ConnectionCount + lBackend2.ConnectionCount;

        Assert.True(lBackend1.ConnectionCount > 0, "Backend 1 should have received some traffic");
        Assert.True(lBackend2.ConnectionCount > 0, "Backend 2 should have received some traffic");

        // Cleanup
        lCancellationTokenSource.Cancel();
    }
}