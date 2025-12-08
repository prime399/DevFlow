using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DevFlow.Models;

public class RequestTab : INotifyPropertyChanged
{
    private Guid _id;
    private string _name = "Untitled";
    private string _httpMethod = "GET";
    private string _url = "https://echo.hoppscotch.io";
    private string _bodyText = string.Empty;
    private ContentType _contentType;
    private bool _overrideContentType;
    private bool _isActive;
    private bool _isDirty;
    private DateTime _createdAt;
    private DateTime _lastModifiedAt;

    public RequestTab()
    {
        _id = Guid.NewGuid();
        _contentType = ContentTypes.ApplicationJson;
        _createdAt = DateTime.UtcNow;
        _lastModifiedAt = DateTime.UtcNow;
        Parameters = new ObservableCollection<RequestParameter> { new RequestParameter() };
        Headers = new ObservableCollection<RequestHeader> { new RequestHeader() };
        Authorization = new AuthorizationConfig();
    }

    public RequestTab(string name) : this()
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
            {
                MarkDirty();
            }
        }
    }

    public string HttpMethod
    {
        get => _httpMethod;
        set
        {
            if (SetProperty(ref _httpMethod, value))
            {
                MarkDirty();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string Url
    {
        get => _url;
        set
        {
            if (SetProperty(ref _url, value))
            {
                MarkDirty();
            }
        }
    }

    public string BodyText
    {
        get => _bodyText;
        set
        {
            if (SetProperty(ref _bodyText, value))
            {
                MarkDirty();
            }
        }
    }

    public ContentType ContentType
    {
        get => _contentType;
        set
        {
            if (SetProperty(ref _contentType, value))
            {
                MarkDirty();
            }
        }
    }

    public bool OverrideContentType
    {
        get => _overrideContentType;
        set
        {
            if (SetProperty(ref _overrideContentType, value))
            {
                MarkDirty();
            }
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

    public ObservableCollection<RequestParameter> Parameters { get; }
    public ObservableCollection<RequestHeader> Headers { get; }
    public AuthorizationConfig Authorization { get; }

    public string DisplayName => $"{HttpMethod} {Name}";

    public string MethodBadgeColor => HttpMethod switch
    {
        "GET" => "#A6E3A1",
        "POST" => "#F9E2AF",
        "PUT" => "#89B4FA",
        "PATCH" => "#CBA6F7",
        "DELETE" => "#F38BA8",
        _ => "#CDD6F4"
    };

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

public class RequestTabManager : INotifyPropertyChanged
{
    private RequestTab? _activeTab;
    private int _tabCounter = 1;

    public RequestTabManager()
    {
        Tabs = new ObservableCollection<RequestTab>();
        AddNewTab();
    }

    public ObservableCollection<RequestTab> Tabs { get; }

    public RequestTab? ActiveTab
    {
        get => _activeTab;
        private set
        {
            if (_activeTab != value)
            {
                if (_activeTab != null)
                {
                    _activeTab.IsActive = false;
                }
                _activeTab = value;
                if (_activeTab != null)
                {
                    _activeTab.IsActive = true;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasActiveTab));
            }
        }
    }

    public bool HasActiveTab => ActiveTab != null;

    public RequestTab AddNewTab(string? name = null)
    {
        var tabName = name ?? $"Untitled";
        var tab = new RequestTab(tabName);
        Tabs.Add(tab);
        _tabCounter++;
        SetActiveTab(tab);
        return tab;
    }

    public RequestTab DuplicateTab(RequestTab source)
    {
        var newTab = new RequestTab($"{source.Name} (Copy)")
        {
            HttpMethod = source.HttpMethod,
            Url = source.Url,
            BodyText = source.BodyText,
            ContentType = source.ContentType,
            OverrideContentType = source.OverrideContentType
        };

        // Copy parameters
        newTab.Parameters.Clear();
        foreach (var param in source.Parameters)
        {
            newTab.Parameters.Add(new RequestParameter(param.ParamKey, param.ParamValue, param.Description, param.IsEnabled));
        }

        // Copy headers
        newTab.Headers.Clear();
        foreach (var header in source.Headers)
        {
            newTab.Headers.Add(new RequestHeader(header.HeaderKey, header.HeaderValue, header.Description, header.IsEnabled));
        }

        // Copy authorization
        newTab.Authorization.AuthType = source.Authorization.AuthType;
        newTab.Authorization.IsEnabled = source.Authorization.IsEnabled;
        newTab.Authorization.Username = source.Authorization.Username;
        newTab.Authorization.Password = source.Authorization.Password;
        newTab.Authorization.BearerToken = source.Authorization.BearerToken;
        newTab.Authorization.ApiKeyName = source.Authorization.ApiKeyName;
        newTab.Authorization.ApiKeyValue = source.Authorization.ApiKeyValue;
        newTab.Authorization.ApiKeyLocation = source.Authorization.ApiKeyLocation;
        newTab.Authorization.AccessToken = source.Authorization.AccessToken;
        newTab.Authorization.TokenType = source.Authorization.TokenType;

        Tabs.Add(newTab);
        SetActiveTab(newTab);
        return newTab;
    }

    public void CloseTab(RequestTab tab)
    {
        var index = Tabs.IndexOf(tab);
        if (index == -1) return;

        Tabs.Remove(tab);

        if (Tabs.Count == 0)
        {
            AddNewTab();
        }
        else if (tab == ActiveTab)
        {
            var newIndex = Math.Min(index, Tabs.Count - 1);
            SetActiveTab(Tabs[newIndex]);
        }
    }

    public void SetActiveTab(RequestTab tab)
    {
        if (Tabs.Contains(tab))
        {
            ActiveTab = tab;
        }
    }

    public void SetActiveTab(int index)
    {
        if (index >= 0 && index < Tabs.Count)
        {
            ActiveTab = Tabs[index];
        }
    }

    public void RenameTab(RequestTab tab, string newName)
    {
        if (Tabs.Contains(tab) && !string.IsNullOrWhiteSpace(newName))
        {
            tab.Name = newName.Trim();
        }
    }

    public void MoveTab(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Tabs.Count ||
            toIndex < 0 || toIndex >= Tabs.Count ||
            fromIndex == toIndex)
            return;

        var tab = Tabs[fromIndex];
        Tabs.RemoveAt(fromIndex);
        Tabs.Insert(toIndex, tab);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
