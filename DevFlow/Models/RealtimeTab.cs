using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DevFlow.Models;

public enum RealtimeProtocol
{
    WebSocket,
    SSE,
    SocketIO
}

public enum LogEntryType
{
    Info,
    Sent,
    Received,
    Error,
    Connected,
    Disconnected
}

public class RealtimeLogEntry : INotifyPropertyChanged
{
    private DateTime _timestamp;
    private LogEntryType _type;
    private string _message = string.Empty;
    private string _details = string.Empty;
    private bool _isExpanded;

    public DateTime Timestamp
    {
        get => _timestamp;
        set => SetProperty(ref _timestamp, value);
    }

    public LogEntryType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public string Details
    {
        get => _details;
        set => SetProperty(ref _details, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    public string TimestampFormatted => Timestamp.ToString("MMM dd, yyyy, h:mm:ss tt");

    public string TypeIcon => Type switch
    {
        LogEntryType.Info => "\uE946",
        LogEntryType.Sent => "\uE724",
        LogEntryType.Received => "\uE896",
        LogEntryType.Error => "\uE783",
        LogEntryType.Connected => "\uE73E",
        LogEntryType.Disconnected => "\uEB55",
        _ => "\uE946"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class RealtimeTab : INotifyPropertyChanged
{
    private Guid _id;
    private string _name = "Untitled";
    private string _url = "wss://ws.postman-echo.com/raw";
    private RealtimeProtocol _protocol = RealtimeProtocol.WebSocket;
    private string _message = "{\n  \"message\": \"Hello\"\n}";
    private bool _isActive;
    private bool _isDirty;
    private bool _isConnected;
    private DateTime _createdAt;
    private DateTime _lastModifiedAt;
    private string _socketIOPath = "/socket.io";
    private string _socketIOVersion = "4";

    public RealtimeTab()
    {
        _id = Guid.NewGuid();
        _createdAt = DateTime.UtcNow;
        _lastModifiedAt = DateTime.UtcNow;
        Logs = new ObservableCollection<RealtimeLogEntry>();
        Protocols = new ObservableCollection<string> { "sub-protocol-1", "sub-protocol-2" };
    }

    public RealtimeTab(string name) : this()
    {
        _name = name;
    }

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                MarkDirty();
        }
    }

    public string Url
    {
        get => _url;
        set
        {
            if (SetProperty(ref _url, value))
                MarkDirty();
        }
    }

    public RealtimeProtocol Protocol
    {
        get => _protocol;
        set
        {
            if (SetProperty(ref _protocol, value))
            {
                MarkDirty();
                UpdateDefaultUrl();
                OnPropertyChanged(nameof(ProtocolName));
            }
        }
    }

    public string ProtocolName => Protocol switch
    {
        RealtimeProtocol.WebSocket => "WebSocket",
        RealtimeProtocol.SSE => "SSE",
        RealtimeProtocol.SocketIO => "Socket.IO",
        _ => "Unknown"
    };

    public string Message
    {
        get => _message;
        set
        {
            if (SetProperty(ref _message, value))
                MarkDirty();
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        set => SetProperty(ref _isDirty, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public DateTime LastModifiedAt
    {
        get => _lastModifiedAt;
        set => SetProperty(ref _lastModifiedAt, value);
    }

    public string SocketIOPath
    {
        get => _socketIOPath;
        set
        {
            if (SetProperty(ref _socketIOPath, value))
                MarkDirty();
        }
    }

    public string SocketIOVersion
    {
        get => _socketIOVersion;
        set
        {
            if (SetProperty(ref _socketIOVersion, value))
                MarkDirty();
        }
    }

    public ObservableCollection<RealtimeLogEntry> Logs { get; }
    public ObservableCollection<string> Protocols { get; }

    private void UpdateDefaultUrl()
    {
        Url = Protocol switch
        {
            RealtimeProtocol.WebSocket => "wss://ws.postman-echo.com/raw",
            RealtimeProtocol.SSE => "https://stream.wikimedia.org/v2/stream/recentchange",
            RealtimeProtocol.SocketIO => "https://socket-io-chat.now.sh",
            _ => Url
        };
    }

    public void AddLog(LogEntryType type, string message, string details = "")
    {
        Logs.Insert(0, new RealtimeLogEntry
        {
            Timestamp = DateTime.Now,
            Type = type,
            Message = message,
            Details = details
        });
    }

    public void ClearLogs()
    {
        Logs.Clear();
    }

    private void MarkDirty()
    {
        IsDirty = true;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void ClearDirty()
    {
        IsDirty = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class RealtimeTabManager : INotifyPropertyChanged
{
    private RealtimeTab? _activeTab;

    public RealtimeTabManager()
    {
        Tabs = new ObservableCollection<RealtimeTab>();
        AddNewTab();
    }

    public ObservableCollection<RealtimeTab> Tabs { get; }

    public RealtimeTab? ActiveTab
    {
        get => _activeTab;
        private set
        {
            if (_activeTab != value)
            {
                if (_activeTab != null)
                    _activeTab.IsActive = false;
                _activeTab = value;
                if (_activeTab != null)
                    _activeTab.IsActive = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasActiveTab));
            }
        }
    }

    public bool HasActiveTab => ActiveTab != null;

    public RealtimeTab AddNewTab(string? name = null)
    {
        var tabName = name ?? "Untitled";
        var tab = new RealtimeTab(tabName);
        Tabs.Add(tab);
        SetActiveTab(tab);
        return tab;
    }

    public void CloseTab(RealtimeTab tab)
    {
        var index = Tabs.IndexOf(tab);
        if (index == -1) return;

        Tabs.Remove(tab);

        if (Tabs.Count == 0)
            AddNewTab();
        else if (tab == ActiveTab)
        {
            var newIndex = Math.Min(index, Tabs.Count - 1);
            SetActiveTab(Tabs[newIndex]);
        }
    }

    public void SetActiveTab(RealtimeTab tab)
    {
        if (Tabs.Contains(tab))
            ActiveTab = tab;
    }

    public void SetActiveTab(int index)
    {
        if (index >= 0 && index < Tabs.Count)
            ActiveTab = Tabs[index];
    }

    public void RenameTab(RealtimeTab tab, string newName)
    {
        if (Tabs.Contains(tab) && !string.IsNullOrWhiteSpace(newName))
            tab.Name = newName.Trim();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
