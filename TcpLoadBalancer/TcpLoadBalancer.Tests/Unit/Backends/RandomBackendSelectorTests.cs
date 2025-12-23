using TcpLoadBalancer.Backends;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Tests.Unit.Backends
{
    [Collection("UnitTests")]
    public class RandomBackendSelectorTests
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
            IBackendSelector lSelector = new RandomBackendSelector(lBackends);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => lSelector.GetNextBackend());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetNextBackend_ReturnsAHealthyBackend()
        {
            // Arrange
            BackendStatus lHealthy = new BackendStatus { Endpoint = new BackendEndpoint { Host = "healthy", Port = 1111 }, IsHealthy = true };
            BackendStatus lUnhealthy = new BackendStatus { Endpoint = new BackendEndpoint { Host = "unhealthy", Port = 2222 }, IsHealthy = false };

            List<BackendStatus> lBackends = new List<BackendStatus> { lUnhealthy, lHealthy };
            IBackendSelector lSelector = new RandomBackendSelector(lBackends);

            // Act
            BackendStatus lChosen = lSelector.GetNextBackend();

            // Assert
            Assert.True(lChosen.IsHealthy);
            Assert.Same(lHealthy, lChosen);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetNextBackend_MultipleCalls_ReturnsOnlyHealthyBackends()
        {
            // Arrange
            BackendStatus a = new BackendStatus { Endpoint = new BackendEndpoint { Host = "a", Port = 1 }, IsHealthy = true };
            BackendStatus b = new BackendStatus { Endpoint = new BackendEndpoint { Host = "b", Port = 2 }, IsHealthy = true };
            BackendStatus c = new BackendStatus { Endpoint = new BackendEndpoint { Host = "c", Port = 3 }, IsHealthy = false };

            List<BackendStatus> lBackends = new List<BackendStatus> { a, b, c };
            IBackendSelector lSelector = new RandomBackendSelector(lBackends);

            // Act & Assert
            for (int i = 0; i < 50; i++)
            {
                BackendStatus lChosen = lSelector.GetNextBackend();
                Assert.True(lChosen.IsHealthy);
                Assert.Contains(lChosen, new List<BackendStatus> { a, b });
            }
        }
    }
}