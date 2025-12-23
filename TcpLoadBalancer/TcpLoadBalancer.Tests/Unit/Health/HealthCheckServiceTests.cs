using TcpLoadBalancer.Health;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Tests.Unit.Health
{
    [Collection("UnitTests")]
    public class HealthCheckServiceTests
    {
        private class TestHealthCheckService : HealthCheckService
        {
            private readonly Dictionary<string, bool> _mockResults;

            public TestHealthCheckService(
                List<BackendStatus> prBackends,
                int prIntervalSeconds,
                CancellationToken prCancellationToken,
                Dictionary<string, bool> prMockResults)
                : base(prBackends, prIntervalSeconds, prCancellationToken)
            {
                _mockResults = prMockResults;
            }

            protected override Task<bool> CheckBackendAsync(BackendStatus prBackendStatus)
            {
                string lKey = $"{prBackendStatus.Endpoint.Host}:{prBackendStatus.Endpoint.Port}";
                return Task.FromResult(_mockResults.ContainsKey(lKey) && _mockResults[lKey]);
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task StartAsync_UpdatesBackendHealthStatus()
        {
            // Arrange
            BackendStatus a = new BackendStatus
            {
                Endpoint = new BackendEndpoint { Host = "a", Port = 1111 },
                IsHealthy = true
            };
            BackendStatus b = new BackendStatus
            {
                Endpoint = new BackendEndpoint { Host = "b", Port = 2222 },
                IsHealthy = false
            };

            List<BackendStatus> lBackends = new List<BackendStatus> { a, b };

            var lMockResults = new Dictionary<string, bool>
            {
                { "a:1111", false },
                { "b:2222", true }
            };

            using CancellationTokenSource lCancellationTokenSource = new CancellationTokenSource();
            TestHealthCheckService lService =
                new TestHealthCheckService(lBackends, 1, lCancellationTokenSource.Token, lMockResults);

            // Act
            Task lTask = lService.StartAsync();
            await Task.Delay(100); // allow one health check cycle
            lCancellationTokenSource.Cancel();
            await lTask;

            // Assert
            Assert.False(a.IsHealthy);
            Assert.True(b.IsHealthy);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task StartAsync_ExitsGracefullyOnCancellationBeforeExecution()
        {
            // Arrange
            BackendStatus lBackendStatus = new BackendStatus
            {
                Endpoint = new BackendEndpoint { Host = "a", Port = 1111 },
                IsHealthy = true
            };

            List<BackendStatus> lBackends = new List<BackendStatus> { lBackendStatus };

            using CancellationTokenSource lCancellationTokenSource = new CancellationTokenSource();
            TestHealthCheckService lService =
                new TestHealthCheckService(lBackends, 1, lCancellationTokenSource.Token, new Dictionary<string, bool>());

            lCancellationTokenSource.Cancel();

            // Act
            await lService.StartAsync();

            // Assert
            Assert.True(true); // test passes if no exception is thrown
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task StartAsync_ExitsGracefullyWhenCancelledDuringExecution()
        {
            // Arrange
            BackendStatus lBackend = new BackendStatus
            {
                Endpoint = new BackendEndpoint { Host = "a", Port = 1111 },
                IsHealthy = true
            };

            List<BackendStatus> lBackends = new List<BackendStatus> { lBackend };

            using var lCancellationTokenSource = new CancellationTokenSource();
            TestHealthCheckService lService =
                new TestHealthCheckService(lBackends, 1000, lCancellationTokenSource.Token, new Dictionary<string, bool>());

            // Act
            Task lServiceTask = lService.StartAsync();
            await Task.Delay(50); // allow service to enter loop
            lCancellationTokenSource.Cancel();
            await lServiceTask;

            // Assert
            Assert.True(true); // test passes if cancellation is handled gracefully
        }
    }
}