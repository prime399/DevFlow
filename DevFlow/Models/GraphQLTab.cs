using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DevFlow.Models;

public class GraphQLTab : INotifyPropertyChanged
{
    private Guid _id;
    private string _name = "Untitled";
    private string _endpoint = "https://echo.hoppscotch.io/graphql";
    private string _query = @"query Request {
  method
  url
  headers {
    key
    value
  }
}";
    private string _variables = "{}";
    private bool _isActive;
    private bool _isDirty;
    private bool _isConnected;
    private DateTime _createdAt;
    private DateTime _lastModifiedAt;

    public GraphQLTab()
    {
        _id = Guid.NewGuid();
        _createdAt = DateTime.UtcNow;
        _lastModifiedAt = DateTime.UtcNow;
        Headers = new ObservableCollection<RequestHeader> { new RequestHeader() };
        Authorization = new AuthorizationConfig();
    }

    public GraphQLTab(string name) : this()
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

    public string Endpoint
    {
        get => _endpoint;
        set
        {
            if (SetProperty(ref _endpoint, value))
                MarkDirty();
        }
    }

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value))
                MarkDirty();
        }
    }

    public string Variables
    {
        get => _variables;
        set
        {
            if (SetProperty(ref _variables, value))
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

    public ObservableCollection<RequestHeader> Headers { get; }
    public AuthorizationConfig Authorization { get; }

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

public class GraphQLTabManager : INotifyPropertyChanged
{
    private GraphQLTab? _activeTab;

    public GraphQLTabManager()
    {
        Tabs = new ObservableCollection<GraphQLTab>();
        AddNewTab();
    }

    public ObservableCollection<GraphQLTab> Tabs { get; }

    public GraphQLTab? ActiveTab
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

    public GraphQLTab AddNewTab(string? name = null)
    {
        var tabName = name ?? "Untitled";
        var tab = new GraphQLTab(tabName);
        Tabs.Add(tab);
        SetActiveTab(tab);
        return tab;
    }

    public void CloseTab(GraphQLTab tab)
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

    public void SetActiveTab(GraphQLTab tab)
    {
        if (Tabs.Contains(tab))
            ActiveTab = tab;
    }

    public void SetActiveTab(int index)
    {
        if (index >= 0 && index < Tabs.Count)
            ActiveTab = Tabs[index];
    }

    public void RenameTab(GraphQLTab tab, string newName)
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
