using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpLoadBalancer.Tests.TestHelpers
{
    public class TestTcpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly List<string> _receivedMessages = new();

        public int Port { get; }
        public IPEndPoint Endpoint => (IPEndPoint)_listener.LocalEndpoint;
        public IReadOnlyList<string> ReceivedMessages => _receivedMessages;

        public TestTcpServer(int port = 0) // 0 = ephemeral port
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _ = AcceptLoop();
        }

        private async Task AcceptLoop()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _ = HandleClient(client);
                }
            }
            catch (OperationCanceledException) { /* expected on dispose */ }
            catch { throw; }
        }

        private async Task HandleClient(TcpClient client)
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var message = await reader.ReadLineAsync();
            if (message != null)
            {
                lock (_receivedMessages)
                    _receivedMessages.Add(message);
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
        }
    }
}
