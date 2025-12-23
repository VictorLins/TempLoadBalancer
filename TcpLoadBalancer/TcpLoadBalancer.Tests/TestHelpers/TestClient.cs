using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

public static class TestClient
{
    public static async Task ConnectAndHoldAsync(IPEndPoint prEndpoint, TimeSpan prDuration, CancellationToken prToken)
    {
        using var lTcpClient = new TcpClient();
        await lTcpClient.ConnectAsync(prEndpoint.Address, prEndpoint.Port, prToken);

        using var lStream = lTcpClient.GetStream();
        var lBuffer = new byte[1];

        var lStopwatch = Stopwatch.StartNew();
        while (lStopwatch.Elapsed < prDuration && !prToken.IsCancellationRequested)
        {
            // Optional: send a heartbeat every second to keep connection alive
            await lStream.WriteAsync(new byte[] { 0 }, 0, 1, prToken);
            await lStream.FlushAsync(prToken);
            await Task.Delay(1000, prToken);
        }
    }
}