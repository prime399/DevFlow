using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DevFlow.Models;

public class RequestHeader : INotifyPropertyChanged
{
    private Guid _id;
    private bool _isEnabled;
    private string _headerKey;
    private string _headerValue;
    private string _description;

    public RequestHeader()
    {
        _id = Guid.NewGuid();
        _isEnabled = true;
        _headerKey = string.Empty;
        _headerValue = string.Empty;
        _description = string.Empty;
    }

    public RequestHeader(string key, string value, string description = "", bool isEnabled = true)
    {
        _id = Guid.NewGuid();
        _isEnabled = isEnabled;
        _headerKey = key;
        _headerValue = value;
        _description = description;
    }

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string HeaderKey
    {
        get => _headerKey;
        set => SetProperty(ref _headerKey, value);
    }

    public string HeaderValue
    {
        get => _headerValue;
        set => SetProperty(ref _headerValue, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
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

public static class CommonHeaderKeys
{
    public static readonly IReadOnlyList<string> All = new[]
    {
        // Authentication
        "Authorization",
        "WWW-Authenticate",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        
        // Caching
        "Age",
        "Cache-Control",
        "Clear-Site-Data",
        "Expires",
        "Pragma",
        
        // Content
        "Accept",
        "Accept-Charset",
        "Accept-Encoding",
        "Accept-Language",
        "Content-Type",
        "Content-Length",
        "Content-Encoding",
        "Content-Language",
        "Content-Location",
        "Content-Disposition",
        
        // CORS
        "Access-Control-Allow-Origin",
        "Access-Control-Allow-Credentials",
        "Access-Control-Allow-Headers",
        "Access-Control-Allow-Methods",
        "Access-Control-Expose-Headers",
        "Access-Control-Max-Age",
        "Access-Control-Request-Headers",
        "Access-Control-Request-Method",
        "Origin",
        
        // Request Context
        "Host",
        "Referer",
        "User-Agent",
        "From",
        
        // Security
        "X-Content-Type-Options",
        "X-Frame-Options",
        "X-XSS-Protection",
        "Strict-Transport-Security",
        "Content-Security-Policy",
        
        // Custom/API
        "X-Api-Key",
        "X-Request-ID",
        "X-Correlation-ID",
        "X-Forwarded-For",
        "X-Forwarded-Host",
        "X-Forwarded-Proto",
        "X-Real-IP",
        
        // Other
        "Cookie",
        "Set-Cookie",
        "Date",
        "ETag",
        "If-Match",
        "If-None-Match",
        "If-Modified-Since",
        "If-Unmodified-Since",
        "Last-Modified",
        "Location",
        "Retry-After"
    };
}
