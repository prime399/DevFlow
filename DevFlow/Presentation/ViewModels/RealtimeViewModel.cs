using System.Text.Json;
using DevFlow.Models;
using DevFlow.Services.Realtime;
using Microsoft.UI.Dispatching;

namespace DevFlow.Presentation.ViewModels;

public partial record RealtimeViewModel
{
    private readonly DispatcherQueue? _dispatcherQueue;
    private IRealtimeConnectionService? _connectionService;

    public RealtimeViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    // Tab Management
    public RealtimeTabManager TabManager { get; } = new RealtimeTabManager();

    // Connection state
    public IState<bool> IsConnecting => State<bool>.Value(this, () => false);

    public async ValueTask ConnectAsync(CancellationToken ct)
    {
        var tab = TabManager.ActiveTab;
        if (tab == null || tab.IsConnected) return;

        if (string.IsNullOrWhiteSpace(tab.Url))
        {
            tab.AddLog(LogEntryType.Error, "Please enter a URL to connect");
            return;
        }

        await IsConnecting.UpdateAsync(_ => true, ct);
        tab.AddLog(LogEntryType.Info, $"Connecting to {tab.Url}...");

        try
        {
            _connectionService = CreateConnectionService(tab.Protocol);
            
            _connectionService.MessageReceived += (s, message) =>
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    var formattedMessage = FormatJsonIfPossible(message);
                    tab.AddLog(LogEntryType.Received, TruncateMessage(message), formattedMessage);
                });
            };

            _connectionService.ErrorOccurred += (s, error) =>
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    tab.AddLog(LogEntryType.Error, "Connection error", error);
                    tab.IsConnected = false;
                });
            };

            _connectionService.Disconnected += (s, e) =>
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    tab.AddLog(LogEntryType.Disconnected, $"Disconnected from {tab.Url}");
                    tab.IsConnected = false;
                });
            };

            await _connectionService.ConnectAsync(tab.Url, ct);
            tab.IsConnected = true;
            tab.AddLog(LogEntryType.Connected, $"Connected to {tab.Url}");
        }
        catch (Exception ex)
        {
            tab.AddLog(LogEntryType.Error, "Connection failed", ex.Message);
            tab.IsConnected = false;
        }
        finally
        {
            await IsConnecting.UpdateAsync(_ => false, ct);
        }
    }

    public async ValueTask DisconnectAsync(CancellationToken ct)
    {
        var tab = TabManager.ActiveTab;
        if (tab == null || !tab.IsConnected || _connectionService == null) return;

        try
        {
            await _connectionService.DisconnectAsync(ct);
            tab.IsConnected = false;
            tab.AddLog(LogEntryType.Disconnected, $"Disconnected from {tab.Url}");
        }
        catch (Exception ex)
        {
            tab.AddLog(LogEntryType.Error, "Disconnect error", ex.Message);
        }
        finally
        {
            if (_connectionService is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _connectionService = null;
        }
    }

    public async ValueTask SendMessageAsync(CancellationToken ct)
    {
        var tab = TabManager.ActiveTab;
        if (tab == null || !tab.IsConnected || _connectionService == null) return;

        var message = tab.Message;
        if (string.IsNullOrEmpty(message)) return;

        try
        {
            if (tab.Protocol == RealtimeProtocol.SocketIO)
            {
                // Socket.IO message format: 42["event",data]
                message = $"42{message}";
            }

            await _connectionService.SendMessageAsync(message, ct);
            tab.AddLog(LogEntryType.Sent, TruncateMessage(tab.Message), FormatJsonIfPossible(tab.Message));
        }
        catch (Exception ex)
        {
            tab.AddLog(LogEntryType.Error, "Send failed", ex.Message);
        }
    }

    private IRealtimeConnectionService CreateConnectionService(RealtimeProtocol protocol)
    {
        return protocol switch
        {
            RealtimeProtocol.WebSocket => new WebSocketConnectionService(),
            RealtimeProtocol.SSE => new SSEConnectionService(),
            RealtimeProtocol.SocketIO => new WebSocketConnectionService(), // Socket.IO uses WebSocket transport
            _ => new WebSocketConnectionService()
        };
    }

    private static string FormatJsonIfPossible(string text)
    {
        return DevFlow.Serialization.JsonHelper.FormatJson(text, relaxedEscaping: false);
    }

    private static string TruncateMessage(string message, int maxLength = 80)
    {
        if (string.IsNullOrEmpty(message)) return message;
        if (message.Length <= maxLength) return message;
        return message.Substring(0, maxLength) + "...";
    }
}
