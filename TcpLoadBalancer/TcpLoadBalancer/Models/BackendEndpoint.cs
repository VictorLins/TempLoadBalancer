using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpLoadBalancer.Models
{
    public class BackendEndpoint
    {
        public string Host { get; init; } = "";
        public int Port { get; init; }
    }
}
