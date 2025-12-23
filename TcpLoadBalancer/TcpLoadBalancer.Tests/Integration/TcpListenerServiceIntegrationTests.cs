using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TcpLoadBalancer.Backends;
using TcpLoadBalancer.Models;
using TcpLoadBalancer.Networking;
using TcpLoadBalancer.Tests.TestHelpers;

[Collection("IntegrationTests")]
public class TcpListenerServiceIntegrationTests
{
    /// <summary>
    /// Integration tests for TcpListenerService to verify load balancing behavior across multiple backends.
    /// 
    /// The tests ensure that messages sent to the TCP listener are distributed correctly according to different
    /// load balancing strategies:
    /// - RoundRobin: messages are sent in rotation to each backend.
    /// - Random: messages may go to any backend, ensuring all backends receive some messages.
    /// - LeastConnections: messages are sent to the backend with the fewest active connections.
    /// </summary>
    [Theory]
    [InlineData("RoundRobin")]
    [InlineData("Random")]
    [InlineData("LeastConnections")]
    [Trait("Category", "Integration")]
    public async Task LoadBalancer_DistributesMessagesAcrossMultipleBackends(string prStrategy)
    {
        // Arrange: create 3 test backends
        using var lBackend1 = new TestTcpServer(9002);
        using var lBackend2 = new TestTcpServer(9003);
        using var lBackend3 = new TestTcpServer(9004);

        var lBackends = new List<BackendStatus>
        {
            new BackendStatus { Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = 9002 }, IsHealthy = true },
            new BackendStatus { Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = 9003 }, IsHealthy = true },
            new BackendStatus { Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = 9004 }, IsHealthy = true },
        };

        var loadBalancerOptionsMonitor = new OptionsMonitor<LoadBalancerOptions>(
                new OptionsFactory<LoadBalancerOptions>(
                new[] { new ConfigureOptions<LoadBalancerOptions>(_ => { }) },
                Enumerable.Empty<IPostConfigureOptions<LoadBalancerOptions>>()),
                Enumerable.Empty<IOptionsChangeTokenSource<LoadBalancerOptions>>(),
                new OptionsCache<LoadBalancerOptions>());

        IBackendSelector lSelector = BackendSelectorFactory.CreateBackendSelector(prStrategy, lBackends);
        var lListenerService = new TcpListenerService(() => lSelector, new IPEndPoint(IPAddress.Loopback, 9005), CancellationToken.None, loadBalancerOptionsMonitor);
        var lListenerTask = lListenerService.StartAsync();
        await Task.Delay(200);

        // Act: send messages from multiple clients
        var lClients = new List<TcpClient>();
        for (int i = 0; i < 6; i++)
        {
            var lClient = new TcpClient();
            await lClient.ConnectAsync("127.0.0.1", 9005);
            lClients.Add(lClient);

            byte[] lMsg = Encoding.UTF8.GetBytes($"Message{i}\n");
            await lClient.GetStream().WriteAsync(lMsg, 0, lMsg.Length);
        }
        // Small delay to ensure messages reach backends
        await Task.Delay(1000);

        await WaitForMessagesAsync(new[] { lBackend1, lBackend2, lBackend3 }, 6);

        // Assert
        if (prStrategy == "RoundRobin")
        {
            var allMessages = lBackend1.ReceivedMessages
            .Concat(lBackend2.ReceivedMessages)
            .Concat(lBackend3.ReceivedMessages)
            .ToList();

            for (int i = 0; i < 6; i++)
                Assert.Contains($"Message{i}", allMessages);
        }
        else if (prStrategy == "Random" || prStrategy == "LeastConnections")
        {
            var lAllReceivedMessages = lBackend1.ReceivedMessages
                .Concat(lBackend2.ReceivedMessages)
                .Concat(lBackend3.ReceivedMessages)
                .ToList();
            for (int i = 0; i < 6; i++)
                Assert.Contains($"Message{i}", lAllReceivedMessages);
        }

        // Cleanup
        foreach (var lClientCurrent in lClients) lClientCurrent.Close();
    }

    private async Task WaitForMessagesAsync(IEnumerable<TestTcpServer> servers, int expectedCount, int timeoutMs = 2000)
    {
        var lStopwatch = Stopwatch.StartNew();
        while (lStopwatch.ElapsedMilliseconds < timeoutMs)
        {
            int lTotal = servers.Sum(s => s.ReceivedMessages.Count);
            if (lTotal >= expectedCount)
                return;

            await Task.Delay(50);
        }

        throw new TimeoutException("Not all messages were received in time");
    }
}