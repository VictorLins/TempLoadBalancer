using TcpLoadBalancer.Health;
using TcpLoadBalancer.Models;
using TcpLoadBalancer.Tests.TestHelpers;

namespace TcpLoadBalancer.Tests.Integration
{
    // <summary>
    /// Integration tests for the HealthCheckService.
    /// 
    /// These tests verify that the HealthCheckService correctly monitors the health of backend servers,
    /// updating their IsHealthy status based on whether they are reachable or not.
    /// </summary>
    /// 
    [Collection("IntegrationTests")]
    public class HealthCheckServiceIntegrationTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task HealthCheckService_UpdatesBackendHealthCorrectly()
        {
            // Arrange
            using var lHealthyBackendServer = new TestTcpServer(9008);
            int lUnhealthyPort = 9009;

            var lBackends = new List<BackendStatus>
            {
                new BackendStatus
                {
                    Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = 9008 },
                    IsHealthy = false // initially unhealthy
                },
                new BackendStatus
                {
                    Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = lUnhealthyPort },
                    IsHealthy = true // initially healthy
                }
            };

            using var lCancellationTokenSource = new CancellationTokenSource();
            var lHealthService = new HealthCheckService(lBackends, prIntervalSeconds: 1, lCancellationTokenSource.Token);

            // Act
            var lHealthTask = lHealthService.StartAsync();
            await Task.Delay(5000);

            // Assert
            Assert.True(lBackends[0].IsHealthy);
            Assert.False(lBackends[1].IsHealthy);

            // Cleanup
            lCancellationTokenSource.Cancel();
            await lHealthTask;
        }
    }
}