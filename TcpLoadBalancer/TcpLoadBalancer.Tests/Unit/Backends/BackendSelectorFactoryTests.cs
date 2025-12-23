using TcpLoadBalancer.Backends;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Tests.Unit.Backends
{
    [Collection("UnitTests")]
    public class BackendSelectorFactoryTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void CreateBackendSelector_UnknownStrategy_ThrowsArgumentException()
        {
            // Arrange
            List<BackendStatus> lBackends = new List<BackendStatus>
            {
                new BackendStatus { Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = 5000 }, IsHealthy = true }
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                BackendSelectorFactory.CreateBackendSelector("NotARealStrategy", lBackends));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetNextBackend_LeastConnections_ReturnsBackendWithFewestActiveConnections()
        {
            // Arrange
            BackendStatus a = new BackendStatus { Endpoint = new BackendEndpoint { Host = "a", Port = 1 }, IsHealthy = true };
            BackendStatus b = new BackendStatus { Endpoint = new BackendEndpoint { Host = "b", Port = 2 }, IsHealthy = true };
            BackendStatus c = new BackendStatus { Endpoint = new BackendEndpoint { Host = "c", Port = 3 }, IsHealthy = true };

            for (int i = 0; i < 10; i++) a.IncrementActiveConnections();
            for (int i = 0; i < 2; i++) b.IncrementActiveConnections();
            for (int i = 0; i < 5; i++) c.IncrementActiveConnections();

            List<BackendStatus> lBackends = new List<BackendStatus> { a, b, c };
            IBackendSelector lSelector = BackendSelectorFactory.CreateBackendSelector("LeastConnections", lBackends);

            // Act
            BackendStatus lChosen = lSelector.GetNextBackend();

            // Assert
            Assert.Same(b, lChosen);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetNextBackend_Random_ReturnsAHealthyBackend()
        {
            // Arrange
            BackendStatus healthy = new BackendStatus { Endpoint = new BackendEndpoint { Host = "healthy", Port = 1111 }, IsHealthy = true };
            BackendStatus unhealthy = new BackendStatus { Endpoint = new BackendEndpoint { Host = "unhealthy", Port = 2222 }, IsHealthy = false };

            List<BackendStatus> lBackends = new List<BackendStatus> { unhealthy, healthy };
            IBackendSelector lSelector = BackendSelectorFactory.CreateBackendSelector("Random", lBackends);

            // Act
            BackendStatus lChosen = lSelector.GetNextBackend();

            // Assert
            Assert.True(lChosen.IsHealthy);
            Assert.Contains(lChosen, lBackends);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetNextBackend_RoundRobin_RotatesThroughHealthyBackends()
        {
            // Arrange
            BackendStatus a = new BackendStatus { Endpoint = new BackendEndpoint { Host = "a", Port = 1 }, IsHealthy = true };
            BackendStatus b = new BackendStatus { Endpoint = new BackendEndpoint { Host = "b", Port = 2 }, IsHealthy = true };

            List<BackendStatus> lBackends = new List<BackendStatus> { a, b };
            IBackendSelector lSelector = BackendSelectorFactory.CreateBackendSelector("RoundRobin", lBackends);

            // Act
            BackendStatus lFirstBackend = lSelector.GetNextBackend();
            BackendStatus lSecondBackend = lSelector.GetNextBackend();
            BackendStatus lThirdBackend = lSelector.GetNextBackend();

            // Assert
            Assert.Same(a, lFirstBackend);
            Assert.Same(b, lSecondBackend);
            Assert.Same(a, lThirdBackend);
        }
    }
}