using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using DevFlow.Models;
using DevFlow.Services.Scripting;
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
    
    // Request Tab Management
    public RequestTabManager TabManager { get; } = new RequestTabManager();
    
    // GraphQL Tab Management
    public GraphQLTabManager GraphQLTabManager { get; } = new GraphQLTabManager();
    
    // Realtime Tab Management
    public RealtimeTabManager RealtimeTabManager { get; } = new RealtimeTabManager();
    
    // Active tab's authorization (for convenience binding)
    public AuthorizationConfig Authorization => TabManager.ActiveTab?.Authorization ?? new AuthorizationConfig();

    public IState<string> SelectedMethod => State<string>.Value(this, () => "GET");
    public IState<string> RequestUrl => State<string>.Value(this, () => "https://echo.hoppscotch.io");
    public IState<string> HeadersText => State<string>.Value(this, () => "accept: application/json");
    public IState<string> BodyText => State<string>.Value(this, () => "{\n  \"method\": \"POST\",\n  \"args\": {},\n  \"data\": \"\",\n  \"headers\": {\n    \"accept\": \"*/*,image/webp\",\n    \"accept-encoding\": \"gzip\"\n  }\n}");
    public IState<ContentType> SelectedContentType => State<ContentType>.Value(this, () => Models.ContentTypes.ApplicationJson);
    public IState<bool> OverrideContentType => State<bool>.Value(this, () => false);
    public IState<int> SelectedTabIndex => State<int>.Value(this, () => 0);
    
    // Pre-request and Post-request scripts
    public IState<string> PreRequestScript => State<string>.Value(this, () => string.Empty);
    public IState<string> PostRequestScript => State<string>.Value(this, () => string.Empty);
    private readonly PreRequestScriptRunner _preScriptRunner = new();
    private readonly PostRequestScriptRunner _postScriptRunner = new();
    
    // Event fired when scripts are executed (for test results UI)
    public event EventHandler<ScriptExecutionResult>? ScriptExecuted;

    // These now reference the active tab's collections
    public ObservableCollection<RequestParameter> Parameters => TabManager.ActiveTab?.Parameters ?? new ObservableCollection<RequestParameter>();
    public ObservableCollection<RequestHeader> Headers => TabManager.ActiveTab?.Headers ?? new ObservableCollection<RequestHeader>();

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

    // GraphQL Response States
    public IState<string> GQLResponseStatus => State<string>.Value(this, () => "Awaiting request");
    public IState<string> GQLResponseTime => State<string>.Value(this, () => "0 ms");
    public IState<string> GQLResponseSize => State<string>.Value(this, () => "0 KB");
    public IState<string> GQLResponseBody => State<string>.Value(this, () => string.Empty);
    public IState<string> GQLErrorMessage => State<string>.Value(this, () => string.Empty);
    public IState<bool> GQLIsSending => State<bool>.Value(this, () => false);
    public ObservableCollection<ResponseHeaderItem> GQLResponseHeaders { get; } = new();

    public async ValueTask SendRequest(CancellationToken ct)
    {
        if (await IsSending)
        {
            return;
        }

        // Execute pre-request script
        ScriptExecutionResult? preScriptResult = null;
        var script = await PreRequestScript;
        if (!string.IsNullOrWhiteSpace(script))
        {
            preScriptResult = _preScriptRunner.Execute(script);
            if (!preScriptResult.IsSuccess)
            {
                await ErrorMessage.UpdateAsync(_ => $"Pre-request script error: {preScriptResult.ErrorMessage}", ct);
                ScriptExecuted?.Invoke(this, preScriptResult);
                return;
            }
        }

        // Get values and resolve environment variables
        var env = ScriptEnvironment.Global;
        var urlInput = env.ResolveVariables(await RequestUrl ?? string.Empty);
        var methodName = await SelectedMethod ?? HttpMethod.Get.Method;
        var queryText = BuildQueryParamsFromCollection();
        var bodyText = env.ResolveVariables(await BodyText ?? string.Empty);
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
            
            // Format JSON for better readability
            var formattedResponse = FormatJsonResponse(responseText);

            await ResponseStatus.UpdateAsync(_ => $"{(int)response.StatusCode} {response.ReasonPhrase}", ct);
            await ResponseTime.UpdateAsync(_ => $"{stopwatch.ElapsedMilliseconds} ms", ct);
            await ResponseSize.UpdateAsync(_ => $"{(responseBytes.Length / 1024d):0.00} KB", ct);
            await ResponseBody.UpdateAsync(_ => formattedResponse, ct);
            
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
            
            // Execute post-request script with response context
            var postScript = await PostRequestScript;
            if (!string.IsNullOrWhiteSpace(postScript))
            {
                var responseContext = new ResponseContext
                {
                    StatusCode = (int)response.StatusCode,
                    Body = responseText,
                    Headers = headersList.ToDictionary(h => h.Key, h => h.Value),
                    ResponseTime = stopwatch.ElapsedMilliseconds,
                    ResponseSize = responseBytes.Length
                };
                
                var postScriptResult = _postScriptRunner.Execute(postScript, responseContext);
                
                // Merge test results from pre and post scripts
                var combinedResult = MergeScriptResults(preScriptResult, postScriptResult);
                ScriptExecuted?.Invoke(this, combinedResult);
            }
            else if (preScriptResult != null)
            {
                ScriptExecuted?.Invoke(this, preScriptResult);
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

    private ScriptExecutionResult MergeScriptResults(ScriptExecutionResult? pre, ScriptExecutionResult post)
    {
        if (pre == null) return post;
        
        var merged = new ScriptExecutionResult
        {
            IsSuccess = pre.IsSuccess && post.IsSuccess,
            ErrorMessage = post.ErrorMessage ?? pre.ErrorMessage,
            ErrorLine = post.ErrorLine > 0 ? post.ErrorLine : pre.ErrorLine,
            Logs = pre.Logs.Concat(post.Logs).ToList(),
            TestResults = pre.TestResults.Concat(post.TestResults).ToList(),
            TotalDuration = pre.TotalDuration + post.TotalDuration
        };
        return merged;
    }

    public async ValueTask SendGraphQLRequest(CancellationToken ct)
    {
        if (await GQLIsSending)
            return;

        var activeTab = GraphQLTabManager.ActiveTab;
        if (activeTab == null)
        {
            await GQLErrorMessage.UpdateAsync(_ => "No active GraphQL tab.", ct);
            return;
        }

        var endpoint = activeTab.Endpoint;
        var query = activeTab.Query;
        var variables = activeTab.Variables;

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            await GQLErrorMessage.UpdateAsync(_ => "Enter a GraphQL endpoint URL.", ct);
            return;
        }

        await GQLErrorMessage.UpdateAsync(_ => string.Empty, ct);
        await GQLIsSending.UpdateAsync(_ => true, ct);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            
            // Add headers from the GraphQL tab's headers collection
            AddGQLHeadersFromCollection(request, activeTab.Headers);
            
            // Add authorization header if enabled
            ApplyGQLAuthorization(request, activeTab.Authorization);

            // Build GraphQL request body
            var gqlBody = new Dictionary<string, object?>
            {
                ["query"] = query
            };

            // Parse and add variables if not empty
            if (!string.IsNullOrWhiteSpace(variables) && variables != "{}")
            {
                try
                {
                    using var varDoc = JsonDocument.Parse(variables);
                    gqlBody["variables"] = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(variables);
                }
                catch (JsonException)
                {
                    // Invalid JSON, send as-is or skip
                }
            }

            var jsonBody = System.Text.Json.JsonSerializer.Serialize(gqlBody);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var httpClient = _httpClientFactory.CreateClient("ApiTester");
            var stopwatch = Stopwatch.StartNew();
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            var responseBytes = await response.Content.ReadAsByteArrayAsync(ct);
            stopwatch.Stop();

            var responseText = await response.Content.ReadAsStringAsync(ct);
            
            // Format JSON for better readability
            var formattedResponse = FormatJsonResponse(responseText);

            await GQLResponseStatus.UpdateAsync(_ => $"{(int)response.StatusCode} {response.ReasonPhrase}", ct);
            await GQLResponseTime.UpdateAsync(_ => $"{stopwatch.ElapsedMilliseconds} ms", ct);
            await GQLResponseSize.UpdateAsync(_ => $"{(responseBytes.Length / 1024d):0.00} KB", ct);
            await GQLResponseBody.UpdateAsync(_ => formattedResponse, ct);
            
            // Populate response headers
            var headersList = new List<ResponseHeaderItem>();
            foreach (var header in response.Headers)
            {
                headersList.Add(new ResponseHeaderItem(header.Key, string.Join(", ", header.Value)));
            }
            foreach (var header in response.Content.Headers)
            {
                headersList.Add(new ResponseHeaderItem(header.Key, string.Join(", ", header.Value)));
            }
            
            var dispatcher = _dispatcherQueue ?? (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow?.DispatcherQueue;
            if (dispatcher != null)
            {
                dispatcher.TryEnqueue(() =>
                {
                    GQLResponseHeaders.Clear();
                    foreach (var h in headersList)
                    {
                        GQLResponseHeaders.Add(h);
                    }
                });
            }
            else
            {
                GQLResponseHeaders.Clear();
                foreach (var h in headersList)
                {
                    GQLResponseHeaders.Add(h);
                }
            }
        }
        catch (Exception ex)
        {
            await GQLErrorMessage.UpdateAsync(_ => ex.Message, ct);
            await GQLResponseStatus.UpdateAsync(_ => "Request failed", ct);
            await GQLResponseTime.UpdateAsync(_ => "0 ms", ct);
            await GQLResponseSize.UpdateAsync(_ => "0 KB", ct);
            await GQLResponseBody.UpdateAsync(_ => string.Empty, ct);
            var dispatcher = _dispatcherQueue ?? (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow?.DispatcherQueue;
            dispatcher?.TryEnqueue(() => GQLResponseHeaders.Clear());
        }
        finally
        {
            await GQLIsSending.UpdateAsync(_ => false, ct);
        }
    }

    private void AddGQLHeadersFromCollection(HttpRequestMessage request, ObservableCollection<RequestHeader> headers)
    {
        var enabledHeaders = headers.Where(h => h.IsEnabled && !string.IsNullOrWhiteSpace(h.HeaderKey));
        foreach (var header in enabledHeaders)
        {
            if (!request.Headers.TryAddWithoutValidation(header.HeaderKey, header.HeaderValue ?? string.Empty))
            {
                request.Content ??= new StringContent(string.Empty);
                _ = request.Content.Headers.TryAddWithoutValidation(header.HeaderKey, header.HeaderValue ?? string.Empty);
            }
        }
    }

    private void ApplyGQLAuthorization(HttpRequestMessage request, AuthorizationConfig auth)
    {
        if (auth == null || !auth.IsEnabled || auth.AuthType == AuthorizationType.None)
            return;

        var authHeader = auth.GenerateAuthorizationHeader();
        if (!string.IsNullOrEmpty(authHeader))
        {
            if (auth.AuthType == AuthorizationType.ApiKey && auth.ApiKeyLocation == ApiKeyLocation.Header)
            {
                request.Headers.TryAddWithoutValidation(auth.ApiKeyName, auth.ApiKeyValue);
            }
            else
            {
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);
            }
        }
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

    private static string FormatJsonResponse(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return responseText;

        var trimmed = responseText.TrimStart();
        if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
            return responseText;

        try
        {
            using var doc = JsonDocument.Parse(responseText);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch (JsonException)
        {
            return responseText;
        }
    }
}
