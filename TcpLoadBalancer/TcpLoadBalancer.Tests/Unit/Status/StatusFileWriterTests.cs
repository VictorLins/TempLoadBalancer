using System.Text.Json;
using TcpLoadBalancer.Models;
using TcpLoadBalancer.Status;

namespace TcpLoadBalancer.Tests.Unit.Status
{
    [Collection("UnitTests")]
    public class StatusFileWriterTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public async Task StartAsync_WritesStatusFile_WhenRunning()
        {
            // Arrange
            BackendStatus lBackend = new BackendStatus
            {
                Endpoint = new BackendEndpoint { Host = "127.0.0.1", Port = 5000 },
                IsHealthy = true
            };
            lBackend.IncrementActiveConnections();

            List<BackendStatus> lBackends = new List<BackendStatus> { lBackend };
            string lTempFile = Path.Combine(Path.GetTempPath(), "status_test.json");
            using CancellationTokenSource lCancellationTokenSource = new CancellationTokenSource();
            var lWriter = new StatusFileWriter(lBackends, lTempFile, 1, lCancellationTokenSource.Token);

            // Act
            Task lWriterTask = lWriter.StartAsync();

            await Task.Delay(1500); // let it write at least once
            lCancellationTokenSource.Cancel();
            await lWriterTask;

            // Assert
            Assert.True(File.Exists(lTempFile));

            string lJsonContent = await File.ReadAllTextAsync(lTempFile);
            var lJsonDoc = JsonDocument.Parse(lJsonContent);

            Assert.True(lJsonDoc.RootElement.TryGetProperty("timestampUtc", out _));
            Assert.True(lJsonDoc.RootElement.TryGetProperty("activeConnections", out var lActiveConnections));
            Assert.Equal(1, lActiveConnections.GetInt32());

            Assert.True(lJsonDoc.RootElement.TryGetProperty("backends", out var lBackendsElement));
            Assert.Equal(1, lBackendsElement.GetArrayLength());

            var lFirstBackend = lBackendsElement[0];
            Assert.Equal("127.0.0.1:5000", lFirstBackend.GetProperty("endpoint").GetString());
            Assert.True(lFirstBackend.GetProperty("healthy").GetBoolean());
            Assert.Equal(1, lFirstBackend.GetProperty("activeConnections").GetInt32());

            // Cleanup
            File.Delete(lTempFile);
        }
    }
}