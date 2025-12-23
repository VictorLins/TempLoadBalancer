using Microsoft.Extensions.Options;
using TcpLoadBalancer.Models;

namespace TcpLoadBalancer.Tests.Unit.Models
{
    internal class LoadBalancerOptionsHelper
    {
        public static OptionsMonitor<LoadBalancerOptions> GetOptionsMonitorFake()
        {
            var lResult = new OptionsMonitor<LoadBalancerOptions>(
                new OptionsFactory<LoadBalancerOptions>(
                    new[] { new ConfigureOptions<LoadBalancerOptions>(_ => { }) },
                    Enumerable.Empty<IPostConfigureOptions<LoadBalancerOptions>>()),
                Enumerable.Empty<IOptionsChangeTokenSource<LoadBalancerOptions>>(),
                new OptionsCache<LoadBalancerOptions>());

            return lResult;
        }
    }
}