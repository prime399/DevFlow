using DevFlow.Models;

namespace DevFlow.Services.Realtime;

public interface IRealtimeConnectionService
{
    bool IsConnected { get; }
    event EventHandler<string>? MessageReceived;
    event EventHandler<string>? ErrorOccurred;
    event EventHandler? Connected;
    event EventHandler? Disconnected;
    
    Task ConnectAsync(string url, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
}

public class ConnectionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
