using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using DevFlow.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace DevFlow.Presentation;

public partial record MainModel
{
    private readonly INavigator _navigator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly ILogger<MainModel>? _logger;

    public MainModel(
        IStringLocalizer localizer,
        IOptions<AppConfig> appInfo,
        INavigator navigator,
        IHttpClientFactory httpClientFactory,
        ILogger<MainModel>? logger = null)
    {
        _navigator = navigator;
        _httpClientFactory = httpClientFactory;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _logger = logger;
        Title = "DevFlow";
        Title += $" - {localizer["ApplicationName"]}";
        Title += $" - {appInfo?.Value?.Environment}";
        
        _logger?.LogInformation("MainModel initialized, DispatcherQueue captured: {HasDispatcher}", _dispatcherQueue != null);
    }

    public string? Title { get; }

    public IReadOnlyList<string> Methods { get; } = new[] { "GET", "POST", "PUT", "PATCH", "DELETE" };
    public IReadOnlyList<ContentType> ContentTypes { get; } = Models.ContentTypes.All;
    public IReadOnlyList<string> CommonHeaders { get; } = CommonHeaderKeys.All;
    public IReadOnlyList<AuthTypeOption> AuthTypes { get; } = AuthTypeInfo.AllTypes;
    public IReadOnlyList<ApiKeyLocationOption> ApiKeyLocations { get; } = ApiKeyLocationInfo.AllLocations;
    
    public AuthorizationConfig Authorization { get; } = new AuthorizationConfig();

    public IState<string> SelectedMethod => State<string>.Value(this, () => "GET");
    public IState<string> RequestUrl => State<string>.Value(this, () => "https://echo.hoppscotch.io");
    public IState<string> HeadersText => State<string>.Value(this, () => "accept: application/json");
    public IState<string> BodyText => State<string>.Value(this, () => "{\n  \"method\": \"POST\",\n  \"args\": {},\n  \"data\": \"\",\n  \"headers\": {\n    \"accept\": \"*/*,image/webp\",\n    \"accept-encoding\": \"gzip\"\n  }\n}");
    public IState<ContentType> SelectedContentType => State<ContentType>.Value(this, () => Models.ContentTypes.ApplicationJson);
    public IState<bool> OverrideContentType => State<bool>.Value(this, () => false);
    public IState<int> SelectedTabIndex => State<int>.Value(this, () => 0);

    public ObservableCollection<RequestParameter> Parameters { get; } = new()
    {
        new RequestParameter("", "", "", true)
    };

    public ObservableCollection<RequestHeader> Headers { get; } = new()
    {
        new RequestHeader("", "", "", true)
    };

    public void AddParameter()
    {
        Parameters.Add(new RequestParameter("", "", "", true));
    }

    public void DeleteParameter(RequestParameter parameter)
    {
        if (Parameters.Count > 1)
        {
            Parameters.Remove(parameter);
        }
        else
        {
            parameter.ParamKey = string.Empty;
            parameter.ParamValue = string.Empty;
            parameter.Description = string.Empty;
            parameter.IsEnabled = true;
        }
    }

    public void ClearAllParameters()
    {
        Parameters.Clear();
        Parameters.Add(new RequestParameter("", "", "", true));
    }

    public void AddHeader()
    {
        Headers.Add(new RequestHeader("", "", "", true));
    }

    public void DeleteHeader(RequestHeader header)
    {
        if (Headers.Count > 1)
        {
            Headers.Remove(header);
        }
        else
        {
            header.HeaderKey = string.Empty;
            header.HeaderValue = string.Empty;
            header.Description = string.Empty;
            header.IsEnabled = true;
        }
    }

    public void ClearAllHeaders()
    {
        Headers.Clear();
        Headers.Add(new RequestHeader("", "", "", true));
    }

    private string BuildQueryParamsFromCollection()
    {
        var enabledParams = Parameters.Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.ParamKey));
        return string.Join("\n", enabledParams.Select(p => $"{p.ParamKey}:{p.ParamValue}"));
    }

    public IState<string> ResponseStatus => State<string>.Value(this, () => "Awaiting request");
    public IState<string> ResponseTime => State<string>.Value(this, () => "0 ms");
    public IState<string> ResponseSize => State<string>.Value(this, () => "0 KB");
    public IState<string> ResponseBody => State<string>.Value(this, () => string.Empty);
    public IState<string> ErrorMessage => State<string>.Value(this, () => string.Empty);
    public IState<bool> IsSending => State<bool>.Value(this, () => false);
    public ObservableCollection<ResponseHeaderItem> ResponseHeaders { get; } = new();

    public async ValueTask SendRequest(CancellationToken ct)
    {
        if (await IsSending)
        {
            return;
        }

        var urlInput = await RequestUrl;
        var methodName = await SelectedMethod ?? HttpMethod.Get.Method;
        var queryText = BuildQueryParamsFromCollection();
        var bodyText = await BodyText ?? string.Empty;
        var contentType = await SelectedContentType;

        if (string.IsNullOrWhiteSpace(urlInput))
        {
            await ErrorMessage.UpdateAsync(_ => "Enter a target URL to send the request.", ct);
            return;
        }

        await ErrorMessage.UpdateAsync(_ => string.Empty, ct);
        await IsSending.UpdateAsync(_ => true, ct);

        try
        {
            var requestUri = BuildUri(urlInput, queryText, Authorization);
            using var request = new HttpRequestMessage(new HttpMethod(methodName), requestUri);
            
            // Add headers from the Headers collection
            AddHeadersFromCollection(request);
            
            // Add authorization header if enabled
            ApplyAuthorization(request);

            if (AllowsBody(methodName) && !string.IsNullOrWhiteSpace(bodyText))
            {
                var mediaType = !string.IsNullOrEmpty(contentType?.Value) ? contentType.Value : "application/json";
                request.Content = new StringContent(bodyText, Encoding.UTF8, mediaType);
            }

            var httpClient = _httpClientFactory.CreateClient("ApiTester");
            var stopwatch = Stopwatch.StartNew();
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            var responseBytes = await response.Content.ReadAsByteArrayAsync(ct);
            stopwatch.Stop();

            var responseText = await response.Content.ReadAsStringAsync(ct);

            await ResponseStatus.UpdateAsync(_ => $"{(int)response.StatusCode} {response.ReasonPhrase}", ct);
            await ResponseTime.UpdateAsync(_ => $"{stopwatch.ElapsedMilliseconds} ms", ct);
            await ResponseSize.UpdateAsync(_ => $"{(responseBytes.Length / 1024d):0.00} KB", ct);
            await ResponseBody.UpdateAsync(_ => responseText, ct);
            
            // Populate response headers on UI thread
            var headersList = new List<ResponseHeaderItem>();
            foreach (var header in response.Headers)
            {
                headersList.Add(new ResponseHeaderItem(header.Key, string.Join(", ", header.Value)));
            }
            foreach (var header in response.Content.Headers)
            {
                headersList.Add(new ResponseHeaderItem(header.Key, string.Join(", ", header.Value)));
            }
            
            _logger?.LogInformation("Collected {Count} response headers", headersList.Count);
            foreach (var h in headersList)
            {
                _logger?.LogInformation("  Header: {Key} = {Value}", h.Key, h.Value);
            }
            
            // Get dispatcher from main window since model might be created on background thread
            var dispatcher = _dispatcherQueue ?? (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow?.DispatcherQueue;
            _logger?.LogInformation("DispatcherQueue available: {Available}, using Window dispatcher: {UsingWindow}", 
                _dispatcherQueue != null, dispatcher != _dispatcherQueue);
            
            if (dispatcher != null)
            {
                var enqueued = dispatcher.TryEnqueue(() =>
                {
                    _logger?.LogInformation("Dispatcher callback executing, adding {Count} headers to collection", headersList.Count);
                    ResponseHeaders.Clear();
                    foreach (var h in headersList)
                    {
                        ResponseHeaders.Add(h);
                    }
                    _logger?.LogInformation("ResponseHeaders collection now has {Count} items", ResponseHeaders.Count);
                });
                _logger?.LogInformation("TryEnqueue result: {Result}", enqueued);
            }
            else
            {
                _logger?.LogWarning("No dispatcher available, updating headers directly");
                ResponseHeaders.Clear();
                foreach (var h in headersList)
                {
                    ResponseHeaders.Add(h);
                }
            }
        }
        catch (Exception ex)
        {
            await ErrorMessage.UpdateAsync(_ => ex.Message, ct);
            await ResponseStatus.UpdateAsync(_ => "Request failed", ct);
            await ResponseTime.UpdateAsync(_ => "0 ms", ct);
            await ResponseSize.UpdateAsync(_ => "0 KB", ct);
            await ResponseBody.UpdateAsync(_ => string.Empty, ct);
            var dispatcher = _dispatcherQueue ?? (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow?.DispatcherQueue;
            dispatcher?.TryEnqueue(() => ResponseHeaders.Clear());
        }
        finally
        {
            await IsSending.UpdateAsync(_ => false, ct);
        }
    }

    public async ValueTask ResetResponse(CancellationToken ct)
    {
        await ResponseStatus.UpdateAsync(_ => "Awaiting request", ct);
        await ResponseTime.UpdateAsync(_ => "0 ms", ct);
        await ResponseSize.UpdateAsync(_ => "0 KB", ct);
        await ResponseBody.UpdateAsync(_ => string.Empty, ct);
        await ErrorMessage.UpdateAsync(_ => string.Empty, ct);
        var dispatcher = _dispatcherQueue ?? (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow?.DispatcherQueue;
        dispatcher?.TryEnqueue(() => ResponseHeaders.Clear());
    }

    private static Uri BuildUri(string urlInput, string queryText, AuthorizationConfig? auth = null)
    {
        var builder = new UriBuilder(urlInput);
        var incomingQuery = BuildQueryString(queryText);
        
        // Add API key as query param if configured
        if (auth?.GetApiKeyQueryParam() is var apiKey && apiKey.HasValue)
        {
            var apiKeyParam = $"{Uri.EscapeDataString(apiKey.Value.Name)}={Uri.EscapeDataString(apiKey.Value.Value)}";
            incomingQuery = string.IsNullOrWhiteSpace(incomingQuery) 
                ? apiKeyParam 
                : $"{incomingQuery}&{apiKeyParam}";
        }
        
        if (!string.IsNullOrWhiteSpace(incomingQuery))
        {
            var existing = builder.Query.TrimStart('?');
            builder.Query = string.IsNullOrWhiteSpace(existing)
                ? incomingQuery
                : $"{existing}&{incomingQuery}";
        }

        return builder.Uri;
    }
    
    private void ApplyAuthorization(HttpRequestMessage request)
    {
        if (Authorization == null || !Authorization.IsEnabled || Authorization.AuthType == AuthorizationType.None)
            return;

        var authHeader = Authorization.GenerateAuthorizationHeader();
        if (!string.IsNullOrEmpty(authHeader))
        {
            // API Key with header location uses custom header name
            if (Authorization.AuthType == AuthorizationType.ApiKey && Authorization.ApiKeyLocation == ApiKeyLocation.Header)
            {
                request.Headers.TryAddWithoutValidation(Authorization.ApiKeyName, Authorization.ApiKeyValue);
            }
            else
            {
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);
            }
        }
    }

    private static string BuildQueryString(string queryText)
    {
        var pairs = ParseKeyValuePairs(queryText);
        var encoded = pairs
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}")
            .ToArray();
        return string.Join("&", encoded);
    }

    private void AddHeadersFromCollection(HttpRequestMessage request)
    {
        var enabledHeaders = Headers.Where(h => h.IsEnabled && !string.IsNullOrWhiteSpace(h.HeaderKey));
        foreach (var header in enabledHeaders)
        {
            if (!request.Headers.TryAddWithoutValidation(header.HeaderKey, header.HeaderValue ?? string.Empty))
            {
                request.Content ??= new StringContent(string.Empty);
                _ = request.Content.Headers.TryAddWithoutValidation(header.HeaderKey, header.HeaderValue ?? string.Empty);
            }
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> ParseKeyValuePairs(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            yield break;
        }

        var lines = rawText.Split(Environment.NewLine);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf(':');
            separatorIndex = separatorIndex < 0 ? trimmed.IndexOf('=') : separatorIndex;
            if (separatorIndex <= 0 || separatorIndex >= trimmed.Length - 1)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            yield return new KeyValuePair<string, string>(key, value);
        }
    }

    private static bool AllowsBody(string methodName) =>
        !string.Equals(methodName, HttpMethod.Get.Method, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(methodName, HttpMethod.Head.Method, StringComparison.OrdinalIgnoreCase);
}
