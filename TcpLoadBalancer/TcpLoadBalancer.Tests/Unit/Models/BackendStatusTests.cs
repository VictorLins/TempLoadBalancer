using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Tests.Unit.Models
{
    [Collection("UnitTests")]
    public class BackendStatusTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void IncrementActiveConnections_WhenCalled_IncreasesActiveConnections()
        {
            // Arrange
            BackendStatus lBackend = new BackendStatus
            {
                Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = 5000 }
            };

            // Act & Assert
            Assert.Equal(0, lBackend.ActiveConnections);

            lBackend.IncrementActiveConnections();
            Assert.Equal(1, lBackend.ActiveConnections);

            lBackend.IncrementActiveConnections();
            Assert.Equal(2, lBackend.ActiveConnections);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void DecrementActiveConnections_WhenCalled_DecreasesActiveConnections()
        {
            // Arrange
            BackendStatus lBackend = new BackendStatus
            {
                Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = 5000 }
            };

            lBackend.IncrementActiveConnections();
            lBackend.IncrementActiveConnections();
            Assert.Equal(2, lBackend.ActiveConnections);

            // Act
            lBackend.DecrementActiveConnections();
            Assert.Equal(1, lBackend.ActiveConnections);

            lBackend.DecrementActiveConnections();

            // Assert
            Assert.Equal(0, lBackend.ActiveConnections);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ActiveConnections_MultipleThreads_IsThreadSafe()
        {
            // Arrange
            BackendStatus lBackend = new BackendStatus
            {
                Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = 5000 }
            };

            int lThreads = 10;
            int lIncrementsPerThread = 1000;

            Thread[] lWorkers = new Thread[lThreads];

            for (int i = 0; i < lThreads; i++)
            {
                lWorkers[i] = new Thread(() =>
                {
                    for (int j = 0; j < lIncrementsPerThread; j++)
                    {
                        lBackend.IncrementActiveConnections();
                    }
                });
            }

            // Act
            foreach (var lThreadCurrent in lWorkers) lThreadCurrent.Start();
            foreach (var lThreadCurrent in lWorkers) lThreadCurrent.Join();

            // Assert
            Assert.Equal(lThreads * lIncrementsPerThread, lBackend.ActiveConnections);
        }
    }
}