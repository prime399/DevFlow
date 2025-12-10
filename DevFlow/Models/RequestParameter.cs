using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DevFlow.Models;

public class RequestParameter : IKeyValueItem
{
    private Guid _id;
    private bool _isEnabled;
    private string _paramKey;
    private string _paramValue;
    private string _description;

    public RequestParameter()
    {
        _id = Guid.NewGuid();
        _isEnabled = true;
        _paramKey = string.Empty;
        _paramValue = string.Empty;
        _description = string.Empty;
    }

    public RequestParameter(string key, string value, string description = "", bool isEnabled = true)
    {
        _id = Guid.NewGuid();
        _isEnabled = isEnabled;
        _paramKey = key;
        _paramValue = value;
        _description = description;
    }

    public Guid Id => _id;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string ParamKey
    {
        get => _paramKey;
        set
        {
            if (SetProperty(ref _paramKey, value))
                OnPropertyChanged(nameof(Key));
        }
    }

    public string ParamValue
    {
        get => _paramValue;
        set
        {
            if (SetProperty(ref _paramValue, value))
                OnPropertyChanged(nameof(Value));
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    // Public Key/Value for unified binding (delegates to ParamKey/ParamValue)
    public string Key
    {
        get => ParamKey;
        set => ParamKey = value;
    }

    public string Value
    {
        get => ParamValue;
        set => ParamValue = value;
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
