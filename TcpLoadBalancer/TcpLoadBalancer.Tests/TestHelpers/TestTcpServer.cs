using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpLoadBalancer.Tests.TestHelpers
{
    public class TestTcpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _CancellationTokenSource = new();
        private readonly List<string> _receivedMessages = new();

        public int Port { get; }
        public IPEndPoint Endpoint => (IPEndPoint)_listener.LocalEndpoint;
        public IReadOnlyList<string> ReceivedMessages => _receivedMessages;

        public TestTcpServer(int prPort = 0)
        {
            _listener = new TcpListener(IPAddress.Loopback, prPort);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _ = AcceptLoop();
        }

        private async Task AcceptLoop()
        {
            try
            {
                while (!_CancellationTokenSource.Token.IsCancellationRequested)
                {
                    var lClient = await _listener.AcceptTcpClientAsync(_CancellationTokenSource.Token);
                    _ = HandleClient(lClient);
                }
            }
            catch (OperationCanceledException) { /* expected on dispose */ }
            catch { throw; }
        }

        private async Task HandleClient(TcpClient client)
        {
            using var Stream = client.GetStream();
            using var lReader = new StreamReader(Stream, Encoding.UTF8);

            var lMessage = await lReader.ReadLineAsync();
            if (lMessage != null)
            {
                lock (_receivedMessages)
                    _receivedMessages.Add(lMessage);
            }
        }

        public void Dispose()
        {
            _CancellationTokenSource.Cancel();
            _listener.Stop();
        }
    }
}