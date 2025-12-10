using System.Net.Http;

namespace DevFlow.Services.Realtime;

public class SSEConnectionService : IRealtimeConnectionService, IDisposable
{
    private HttpClient? _httpClient;
    private HttpResponseMessage? _response;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private bool _isConnected;

    public bool IsConnected => _isConnected;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public async Task ConnectAsync(string url, CancellationToken cancellationToken = default)
    {
        if (_isConnected)
            throw new InvalidOperationException("Already connected");

        _httpClient = new HttpClient();
        _receiveCts = new CancellationTokenSource();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            _response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            _response.EnsureSuccessStatusCode();

            _isConnected = true;
            Connected?.Invoke(this, EventArgs.Empty);

            _receiveTask = Task.Run(() => ReceiveMessagesAsync(_receiveCts.Token));
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
            throw;
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _receiveCts?.Cancel();
        _response?.Dispose();
        _response = null;
        _httpClient?.Dispose();
        _httpClient = null;
        _isConnected = false;
        Disconnected?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        // SSE is receive-only
        throw new NotSupportedException("SSE connections are receive-only");
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        if (_response == null) return;

        try
        {
            using var stream = await _response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string? eventType = null;
            var dataLines = new List<string>();

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                
                if (line == null) // End of stream
                    break;

                if (string.IsNullOrEmpty(line))
                {
                    // Empty line indicates end of event
                    if (dataLines.Count > 0)
                    {
                        var data = string.Join("\n", dataLines);
                        var message = eventType != null ? $"event: {eventType}\ndata: {data}" : data;
                        MessageReceived?.Invoke(this, message);
                        dataLines.Clear();
                        eventType = null;
                    }
                    continue;
                }

                if (line.StartsWith("data:"))
                {
                    dataLines.Add(line.Substring(5).TrimStart());
                }
                else if (line.StartsWith("event:"))
                {
                    eventType = line.Substring(6).TrimStart();
                }
                // Ignore other fields like id:, retry:, comments
            }

            _isConnected = false;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            _isConnected = false;
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    public void Dispose()
    {
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        _response?.Dispose();
        _httpClient?.Dispose();
    }
}
