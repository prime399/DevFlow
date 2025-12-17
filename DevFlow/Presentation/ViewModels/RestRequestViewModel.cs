using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using DevFlow.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace DevFlow.Presentation.ViewModels;

public partial record RestRequestViewModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly ILogger? _logger;

    public RestRequestViewModel(
        IHttpClientFactory httpClientFactory,
        ILogger? logger = null)
    {
        _httpClientFactory = httpClientFactory;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _logger = logger;
    }

    public IReadOnlyList<string> Methods { get; } = new[] { "GET", "POST", "PUT", "PATCH", "DELETE" };
    public IReadOnlyList<ContentType> ContentTypes { get; } = Models.ContentTypes.All;
    public IReadOnlyList<string> CommonHeaders { get; } = CommonHeaderKeys.All;
    public IReadOnlyList<AuthTypeOption> AuthTypes { get; } = AuthTypeInfo.AllTypes;
    public IReadOnlyList<ApiKeyLocationOption> ApiKeyLocations { get; } = ApiKeyLocationInfo.AllLocations;

    // Request Tab Management
    public RequestTabManager TabManager { get; } = new RequestTabManager();

    // Active tab's authorization (for convenience binding)
    public AuthorizationConfig Authorization => TabManager.ActiveTab?.Authorization ?? new AuthorizationConfig();

    public IState<string> SelectedMethod => State<string>.Value(this, () => "GET");
    public IState<string> RequestUrl => State<string>.Value(this, () => "https://echo.hoppscotch.io");
    public IState<string> BodyText => State<string>.Value(this, () => "{\n  \"method\": \"POST\"\n}");
    public IState<ContentType> SelectedContentType => State<ContentType>.Value(this, () => Models.ContentTypes.ApplicationJson);
    public IState<bool> OverrideContentType => State<bool>.Value(this, () => false);
    public IState<int> SelectedTabIndex => State<int>.Value(this, () => 0);

    // These now reference the active tab's collections
    public ObservableCollection<RequestParameter> Parameters => TabManager.ActiveTab?.Parameters ?? new ObservableCollection<RequestParameter>();
    public ObservableCollection<RequestHeader> Headers => TabManager.ActiveTab?.Headers ?? new ObservableCollection<RequestHeader>();

    // Response states
    public IState<string> ResponseStatus => State<string>.Value(this, () => "Awaiting request");
    public IState<string> ResponseTime => State<string>.Value(this, () => "0 ms");
    public IState<string> ResponseSize => State<string>.Value(this, () => "0 KB");
    public IState<string> ResponseBody => State<string>.Value(this, () => string.Empty);
    public IState<string> ErrorMessage => State<string>.Value(this, () => string.Empty);
    public IState<bool> IsSending => State<bool>.Value(this, () => false);
    public ObservableCollection<ResponseHeaderItem> ResponseHeaders { get; } = new();

    public void AddParameter() => Parameters.Add(new RequestParameter("", "", "", true));

    public void DeleteParameter(RequestParameter parameter)
    {
        if (Parameters.Count > 1)
            Parameters.Remove(parameter);
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

    public void AddHeader() => Headers.Add(new RequestHeader("", "", "", true));

    public void DeleteHeader(RequestHeader header)
    {
        if (Headers.Count > 1)
            Headers.Remove(header);
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

    public async ValueTask SendRequest(CancellationToken ct)
    {
        if (await IsSending) return;

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

            AddHeadersFromCollection(request);
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
            var formattedResponse = FormatJsonResponse(responseText);

            await ResponseStatus.UpdateAsync(_ => $"{(int)response.StatusCode} {response.ReasonPhrase}", ct);
            await ResponseTime.UpdateAsync(_ => $"{stopwatch.ElapsedMilliseconds} ms", ct);
            await ResponseSize.UpdateAsync(_ => $"{(responseBytes.Length / 1024d):0.00} KB", ct);
            await ResponseBody.UpdateAsync(_ => formattedResponse, ct);

            var headersList = new List<ResponseHeaderItem>();
            foreach (var header in response.Headers)
                headersList.Add(new ResponseHeaderItem(header.Key, string.Join(", ", header.Value)));
            foreach (var header in response.Content.Headers)
                headersList.Add(new ResponseHeaderItem(header.Key, string.Join(", ", header.Value)));

            var dispatcher = _dispatcherQueue ?? (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow?.DispatcherQueue;
            if (dispatcher != null)
            {
                dispatcher.TryEnqueue(() =>
                {
                    ResponseHeaders.Clear();
                    foreach (var h in headersList)
                        ResponseHeaders.Add(h);
                });
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

        if (auth?.GetApiKeyQueryParam() is var apiKey && apiKey.HasValue)
        {
            var apiKeyParam = $"{Uri.EscapeDataString(apiKey.Value.Name)}={Uri.EscapeDataString(apiKey.Value.Value)}";
            incomingQuery = string.IsNullOrWhiteSpace(incomingQuery) ? apiKeyParam : $"{incomingQuery}&{apiKeyParam}";
        }

        if (!string.IsNullOrWhiteSpace(incomingQuery))
        {
            var existing = builder.Query.TrimStart('?');
            builder.Query = string.IsNullOrWhiteSpace(existing) ? incomingQuery : $"{existing}&{incomingQuery}";
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
            if (Authorization.AuthType == AuthorizationType.ApiKey && Authorization.ApiKeyLocation == ApiKeyLocation.Header)
                request.Headers.TryAddWithoutValidation(Authorization.ApiKeyName, Authorization.ApiKeyValue);
            else
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }
    }

    private static string BuildQueryString(string queryText)
    {
        var pairs = ParseKeyValuePairs(queryText);
        var encoded = pairs.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}").ToArray();
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
        if (string.IsNullOrWhiteSpace(rawText)) yield break;

        var lines = rawText.Split(Environment.NewLine);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            var separatorIndex = trimmed.IndexOf(':');
            separatorIndex = separatorIndex < 0 ? trimmed.IndexOf('=') : separatorIndex;
            if (separatorIndex <= 0 || separatorIndex >= trimmed.Length - 1) continue;

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrEmpty(key)) continue;

            yield return new KeyValuePair<string, string>(key, value);
        }
    }

    private static bool AllowsBody(string methodName) =>
        !string.Equals(methodName, HttpMethod.Get.Method, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(methodName, HttpMethod.Head.Method, StringComparison.OrdinalIgnoreCase);

    private static string FormatJsonResponse(string responseText)
    {
        return DevFlow.Serialization.JsonHelper.FormatJson(responseText);
    }
}
