using System.Net;
using System.Net.Sockets;

public class FakeBackendServer : IAsyncDisposable
{
    private TcpListener _listener;
    public int ConnectionCount;
    private readonly int _port;

    public FakeBackendServer(int port) => _port = port;

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _ = Task.Run(AcceptLoop);
    }

    private async Task AcceptLoop()
    {
        while (true)
        {
            try
            {
                var lClient = await _listener.AcceptTcpClientAsync();
                Interlocked.Increment(ref ConnectionCount);
                // Keep the connection open but don't block the loop
                _ = Task.Run(() => HandleClient(lClient));
            }
            catch { break; }
        }
    }

    private async Task HandleClient(TcpClient prClient)
    {
        using (prClient)
        {
            var lBuffer = new byte[1024];
            while (await prClient.GetStream().ReadAsync(lBuffer) > 0) { /* Just drain the stream */ }
        }
    }

    public async ValueTask DisposeAsync() => _listener?.Stop();
}