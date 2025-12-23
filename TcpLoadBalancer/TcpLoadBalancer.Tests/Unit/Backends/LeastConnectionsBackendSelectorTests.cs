using TcpLoadBalancer.Backends;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Tests.Unit.Backends
{
    [Collection("UnitTests")]
    public class LeastConnectionsBackendSelectorTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void GetNextBackend_NoHealthyBackends_ThrowsInvalidOperationException()
        {
            // Arrange
            List<BackendStatus> lBackends = new List<BackendStatus>
            {
                new BackendStatus { Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = 5000 }, IsHealthy = false },
                new BackendStatus { Endpoint = new BackendEndpoint { Host = "127.0.0.2", Port = 5001 }, IsHealthy = false }
            };
            IBackendSelector lSelector = new LeastConnectionsBackendSelector(lBackends);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => lSelector.GetNextBackend());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetNextBackend_ReturnsBackendWithFewestActiveConnections()
        {
            // Arrange
            BackendStatus a = new BackendStatus { Endpoint = new BackendEndpoint { Host = "a", Port = 1 }, IsHealthy = true };
            BackendStatus b = new BackendStatus { Endpoint = new BackendEndpoint { Host = "b", Port = 2 }, IsHealthy = true };
            BackendStatus c = new BackendStatus { Endpoint = new BackendEndpoint { Host = "c", Port = 3 }, IsHealthy = true };

            for (int i = 0; i < 10; i++) a.IncrementActiveConnections();
            for (int i = 0; i < 2; i++) b.IncrementActiveConnections();
            for (int i = 0; i < 5; i++) c.IncrementActiveConnections();

            List<BackendStatus> lBackends = new List<BackendStatus> { a, b, c };
            IBackendSelector lSelector = new LeastConnectionsBackendSelector(lBackends);

            // Act
            BackendStatus lChosen = lSelector.GetNextBackend();

            // Assert
            Assert.Same(b, lChosen);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetNextBackend_IgnoresUnhealthyBackends()
        {
            // Arrange
            BackendStatus healthy = new BackendStatus { Endpoint = new BackendEndpoint { Host = "healthy", Port = 1111 }, IsHealthy = true };
            BackendStatus unhealthy = new BackendStatus { Endpoint = new BackendEndpoint { Host = "unhealthy", Port = 2222 }, IsHealthy = false };

            List<BackendStatus> lBackends = new List<BackendStatus> { unhealthy, healthy };
            IBackendSelector lSelector = new LeastConnectionsBackendSelector(lBackends);

            // Act
            BackendStatus lChosen = lSelector.GetNextBackend();

            // Assert
            Assert.True(lChosen.IsHealthy);
            Assert.Same(healthy, lChosen);
        }
    }
}