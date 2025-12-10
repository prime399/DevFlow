using System.Net.WebSockets;
using System.Text;

namespace DevFlow.Services.Realtime;

public class WebSocketConnectionService : IRealtimeConnectionService, IDisposable
{
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public async Task ConnectAsync(string url, CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            throw new InvalidOperationException("Already connected");

        _webSocket = new ClientWebSocket();
        _receiveCts = new CancellationTokenSource();

        try
        {
            await _webSocket.ConnectAsync(new Uri(url), cancellationToken);
            Connected?.Invoke(this, EventArgs.Empty);
            _receiveTask = Task.Run(() => ReceiveMessagesAsync(_receiveCts.Token));
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket == null) return;

        try
        {
            _receiveCts?.Cancel();

            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", cancellationToken);
            }
        }
        finally
        {
            _webSocket?.Dispose();
            _webSocket = null;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_webSocket?.State != WebSocketState.Open)
            throw new InvalidOperationException("Not connected");

        var bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var messageBuffer = new StringBuilder();

        try
        {
            while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Disconnected?.Invoke(this, EventArgs.Empty);
                    break;
                }

                var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messageBuffer.Append(chunk);

                if (result.EndOfMessage)
                {
                    var message = messageBuffer.ToString();
                    messageBuffer.Clear();
                    MessageReceived?.Invoke(this, message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    public void Dispose()
    {
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        _webSocket?.Dispose();
    }
}
