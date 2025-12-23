using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Net.Sockets;
using TcpLoadBalancer.Backends;
using TcpLoadBalancer.Models;
using TcpLoadBalancer.Networking;

namespace TcpLoadBalancer.Tests.Unit.Networking
{
    [Collection("UnitTests")]
    public class TcpListenerServiceTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public async Task StartAsync_CancellationRequested_StopsGracefully()
        {
            // Arrange
            var lMockBackendSelector = new Mock<IBackendSelector>();
            using CancellationTokenSource lCancellationTokenSource = new CancellationTokenSource();

            var lLoadBalancerOptionsMonitor = new OptionsMonitor<LoadBalancerOptions>(
                new OptionsFactory<LoadBalancerOptions>(
                new[] { new ConfigureOptions<LoadBalancerOptions>(_ => { }) },
                Enumerable.Empty<IPostConfigureOptions<LoadBalancerOptions>>()),
                Enumerable.Empty<IOptionsChangeTokenSource<LoadBalancerOptions>>(),
                new OptionsCache<LoadBalancerOptions>());

            var lEndpoint = new IPEndPoint(IPAddress.Loopback, 0); // OS assigns free port
            var lService = new TcpListenerService(() => lMockBackendSelector.Object, lEndpoint, lCancellationTokenSource.Token, lLoadBalancerOptionsMonitor);

            // Act
            Task lServiceTask = lService.StartAsync();
            await Task.Delay(100); // allow listener to start
            lCancellationTokenSource.Cancel();

            // Assert
            var lException = await Record.ExceptionAsync(() => lServiceTask);
            Assert.Null(lException);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task StartAsync_ClientConnects_CallsGetNextBackend()
        {
            // Arrange
            var lMockBackendSelector = new Mock<IBackendSelector>();
            using CancellationTokenSource lCancellationTokenSource = new CancellationTokenSource();

            var lLoadBalancerOptionsMonitor = new OptionsMonitor<LoadBalancerOptions>(
               new OptionsFactory<LoadBalancerOptions>(
               new[] { new ConfigureOptions<LoadBalancerOptions>(_ => { }) },
               Enumerable.Empty<IPostConfigureOptions<LoadBalancerOptions>>()),
               Enumerable.Empty<IOptionsChangeTokenSource<LoadBalancerOptions>>(),
               new OptionsCache<LoadBalancerOptions>());

            int lTestPort = 5000;
            var lEndpoint = new IPEndPoint(IPAddress.Loopback, lTestPort);
            var lService = new TcpListenerService(() => lMockBackendSelector.Object, lEndpoint, lCancellationTokenSource.Token, lLoadBalancerOptionsMonitor);

            // Act
            Task lServiceTask = lService.StartAsync();
            await Task.Delay(100); // wait for listener to start

            using (var lClient = new TcpClient())
            {
                await lClient.ConnectAsync(IPAddress.Loopback, lTestPort);
                await Task.Delay(200); // allow service to call selector
            }

            lCancellationTokenSource.Cancel();
            await lServiceTask;

            // Assert
            lMockBackendSelector.Verify(m => m.GetNextBackend(), Times.AtLeastOnce());
        }
    }
}