using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DevFlow.Models;

public enum AuthorizationType
{
    None,
    BasicAuth,
    BearerToken,
    ApiKey,
    DigestAuth,
    OAuth2
}

public enum ApiKeyLocation
{
    Header,
    QueryParam
}

public class AuthorizationConfig : INotifyPropertyChanged
{
    private AuthorizationType _authType = AuthorizationType.None;
    private bool _isEnabled = true;
    
    // Basic Auth
    private string _username = string.Empty;
    private string _password = string.Empty;
    
    // Bearer Token
    private string _bearerToken = string.Empty;
    
    // API Key
    private string _apiKeyName = string.Empty;
    private string _apiKeyValue = string.Empty;
    private ApiKeyLocation _apiKeyLocation = ApiKeyLocation.Header;
    
    // OAuth 2.0
    private string _accessToken = string.Empty;
    private string _tokenType = "Bearer";
    private string _clientId = string.Empty;
    private string _clientSecret = string.Empty;
    private string _authorizationEndpoint = string.Empty;
    private string _tokenEndpoint = string.Empty;
    private string _scope = string.Empty;

    public AuthorizationType AuthType
    {
        get => _authType;
        set => SetProperty(ref _authType, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    // Basic Auth Properties
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    // Bearer Token Properties
    public string BearerToken
    {
        get => _bearerToken;
        set => SetProperty(ref _bearerToken, value);
    }

    // API Key Properties
    public string ApiKeyName
    {
        get => _apiKeyName;
        set => SetProperty(ref _apiKeyName, value);
    }

    public string ApiKeyValue
    {
        get => _apiKeyValue;
        set => SetProperty(ref _apiKeyValue, value);
    }

    public ApiKeyLocation ApiKeyLocation
    {
        get => _apiKeyLocation;
        set => SetProperty(ref _apiKeyLocation, value);
    }

    // OAuth 2.0 Properties
    public string AccessToken
    {
        get => _accessToken;
        set => SetProperty(ref _accessToken, value);
    }

    public string TokenType
    {
        get => _tokenType;
        set => SetProperty(ref _tokenType, value);
    }

    public string ClientId
    {
        get => _clientId;
        set => SetProperty(ref _clientId, value);
    }

    public string ClientSecret
    {
        get => _clientSecret;
        set => SetProperty(ref _clientSecret, value);
    }

    public string AuthorizationEndpoint
    {
        get => _authorizationEndpoint;
        set => SetProperty(ref _authorizationEndpoint, value);
    }

    public string TokenEndpoint
    {
        get => _tokenEndpoint;
        set => SetProperty(ref _tokenEndpoint, value);
    }

    public string Scope
    {
        get => _scope;
        set => SetProperty(ref _scope, value);
    }

    public string GenerateAuthorizationHeader()
    {
        if (!IsEnabled)
            return string.Empty;

        return AuthType switch
        {
            AuthorizationType.BasicAuth => GenerateBasicAuthHeader(),
            AuthorizationType.BearerToken => GenerateBearerHeader(),
            AuthorizationType.ApiKey when ApiKeyLocation == ApiKeyLocation.Header => $"{ApiKeyName}: {ApiKeyValue}",
            AuthorizationType.OAuth2 => GenerateOAuth2Header(),
            _ => string.Empty
        };
    }

    private string GenerateBasicAuthHeader()
    {
        if (string.IsNullOrEmpty(Username))
            return string.Empty;
        
        var credentials = $"{Username}:{Password}";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        return $"Basic {base64}";
    }

    private string GenerateBearerHeader()
    {
        if (string.IsNullOrEmpty(BearerToken))
            return string.Empty;
        
        return $"Bearer {BearerToken}";
    }

    private string GenerateOAuth2Header()
    {
        if (string.IsNullOrEmpty(AccessToken))
            return string.Empty;
        
        var type = string.IsNullOrEmpty(TokenType) ? "Bearer" : TokenType;
        return $"{type} {AccessToken}";
    }

    public (string Name, string Value)? GetApiKeyQueryParam()
    {
        if (!IsEnabled || AuthType != AuthorizationType.ApiKey || ApiKeyLocation != ApiKeyLocation.QueryParam)
            return null;
        
        if (string.IsNullOrEmpty(ApiKeyName) || string.IsNullOrEmpty(ApiKeyValue))
            return null;
        
        return (ApiKeyName, ApiKeyValue);
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

public static class AuthTypeInfo
{
    public static readonly IReadOnlyList<AuthTypeOption> AllTypes = new[]
    {
        new AuthTypeOption(AuthorizationType.None, "None", "No authentication"),
        new AuthTypeOption(AuthorizationType.BasicAuth, "Basic Auth", "Username and password sent in Base64 encoding"),
        new AuthTypeOption(AuthorizationType.BearerToken, "Bearer Token", "Token-based authentication"),
        new AuthTypeOption(AuthorizationType.ApiKey, "API Key", "API key in header or query parameter"),
        new AuthTypeOption(AuthorizationType.OAuth2, "OAuth 2.0", "OAuth 2.0 access token authentication")
    };
}

public record AuthTypeOption(AuthorizationType Type, string Name, string Description);

public static class ApiKeyLocationInfo
{
    public static readonly IReadOnlyList<ApiKeyLocationOption> AllLocations = new[]
    {
        new ApiKeyLocationOption(ApiKeyLocation.Header, "Header"),
        new ApiKeyLocationOption(ApiKeyLocation.QueryParam, "Query Params")
    };
}

public record ApiKeyLocationOption(ApiKeyLocation Location, string Name);
