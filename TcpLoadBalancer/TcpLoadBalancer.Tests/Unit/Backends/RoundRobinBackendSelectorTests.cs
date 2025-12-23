using TcpLoadBalancer.Backends;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Tests.Unit.Backends
{
    [Collection("UnitTests")]
    public class RoundRobinBackendSelectorTests
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

            IBackendSelector lSelector = new RoundRobinBackendSelector(lBackends);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => lSelector.GetNextBackend());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetNextBackend_RotatesThroughHealthyBackends()
        {
            // Arrange
            BackendStatus a = new BackendStatus { Endpoint = new BackendEndpoint { Host = "a", Port = 1 }, IsHealthy = true };
            BackendStatus b = new BackendStatus { Endpoint = new BackendEndpoint { Host = "b", Port = 2 }, IsHealthy = true };

            List<BackendStatus> lBackends = new List<BackendStatus> { a, b };
            IBackendSelector lSelector = new RoundRobinBackendSelector(lBackends);

            // Act
            BackendStatus lFirstBackend = lSelector.GetNextBackend();
            BackendStatus lSecondBackend = lSelector.GetNextBackend();
            BackendStatus lThirdBackend = lSelector.GetNextBackend();

            // Assert
            Assert.Same(a, lFirstBackend);
            Assert.Same(b, lSecondBackend);
            Assert.Same(a, lThirdBackend);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetNextBackend_SkipsUnhealthyBackends()
        {
            // Arrange
            BackendStatus lHealthy1 = new BackendStatus { Endpoint = new BackendEndpoint { Host = "healthy1", Port = 1111 }, IsHealthy = true };
            BackendStatus lUnhealthy = new BackendStatus { Endpoint = new BackendEndpoint { Host = "unhealthy", Port = 2222 }, IsHealthy = false };
            BackendStatus lHealthy2 = new BackendStatus { Endpoint = new BackendEndpoint { Host = "healthy2", Port = 3333 }, IsHealthy = true };

            List<BackendStatus> lBackends = new List<BackendStatus> { lHealthy1, lUnhealthy, lHealthy2 };
            IBackendSelector lSelector = new RoundRobinBackendSelector(lBackends);

            // Act
            BackendStatus lFirst = lSelector.GetNextBackend();
            BackendStatus lSecond = lSelector.GetNextBackend();
            BackendStatus lThird = lSelector.GetNextBackend();

            // Assert
            Assert.Same(lHealthy1, lFirst);
            Assert.Same(lHealthy2, lSecond);
            Assert.Same(lHealthy1, lThird); // skips unhealthy and continues rotation
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetNextBackend_MultipleThreads_RotatesCorrectly()
        {
            // Arrange
            BackendStatus a = new BackendStatus { Endpoint = new BackendEndpoint { Host = "a", Port = 1 }, IsHealthy = true };
            BackendStatus b = new BackendStatus { Endpoint = new BackendEndpoint { Host = "b", Port = 2 }, IsHealthy = true };

            List<BackendStatus> lValidBackends = new List<BackendStatus> { a, b };
            IBackendSelector lSelector = new RoundRobinBackendSelector(lValidBackends);

            BackendStatus[] lBackendResults = new BackendStatus[10];

            // Act
            Parallel.For(0, 10, i =>
            {
                lBackendResults[i] = lSelector.GetNextBackend();
            });

            // Assert
            foreach (var lBackendCurrent in lBackendResults)
            {
                Assert.True(lBackendCurrent.IsHealthy);
                Assert.Contains(lBackendCurrent, lValidBackends);
            }
        }
    }
}