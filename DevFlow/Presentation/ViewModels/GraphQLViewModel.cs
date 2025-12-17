using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using DevFlow.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace DevFlow.Presentation.ViewModels;

public partial record GraphQLViewModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly ILogger? _logger;

    public GraphQLViewModel(
        IHttpClientFactory httpClientFactory,
        ILogger? logger = null)
    {
        _httpClientFactory = httpClientFactory;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _logger = logger;
    }

    public IReadOnlyList<AuthTypeOption> AuthTypes { get; } = AuthTypeInfo.AllTypes;
    public IReadOnlyList<ApiKeyLocationOption> ApiKeyLocations { get; } = ApiKeyLocationInfo.AllLocations;

    // GraphQL Tab Management
    public GraphQLTabManager TabManager { get; } = new GraphQLTabManager();

    // Response States
    public IState<string> ResponseStatus => State<string>.Value(this, () => "Awaiting request");
    public IState<string> ResponseTime => State<string>.Value(this, () => "0 ms");
    public IState<string> ResponseSize => State<string>.Value(this, () => "0 KB");
    public IState<string> ResponseBody => State<string>.Value(this, () => string.Empty);
    public IState<string> ErrorMessage => State<string>.Value(this, () => string.Empty);
    public IState<bool> IsSending => State<bool>.Value(this, () => false);
    public ObservableCollection<ResponseHeaderItem> ResponseHeaders { get; } = new();

    public async ValueTask SendRequest(CancellationToken ct)
    {
        if (await IsSending) return;

        var activeTab = TabManager.ActiveTab;
        if (activeTab == null)
        {
            await ErrorMessage.UpdateAsync(_ => "No active GraphQL tab.", ct);
            return;
        }

        var endpoint = activeTab.Endpoint;
        var query = activeTab.Query;
        var variables = activeTab.Variables;

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            await ErrorMessage.UpdateAsync(_ => "Enter a GraphQL endpoint URL.", ct);
            return;
        }

        await ErrorMessage.UpdateAsync(_ => string.Empty, ct);
        await IsSending.UpdateAsync(_ => true, ct);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

            AddHeadersFromCollection(request, activeTab.Headers);
            ApplyAuthorization(request, activeTab.Authorization);

            var gqlBody = new Dictionary<string, object?> { ["query"] = query };

            if (!string.IsNullOrWhiteSpace(variables) && variables != "{}")
            {
                try
                {
                    var parsedVars = DevFlow.Serialization.JsonHelper.ParseVariables(variables);
                    if (parsedVars.HasValue)
                    {
                        gqlBody["variables"] = parsedVars.Value;
                    }
                }
                catch (JsonException) { }
            }

            var jsonBody = DevFlow.Serialization.JsonHelper.SerializeDictionary(gqlBody);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

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

    private void AddHeadersFromCollection(HttpRequestMessage request, ObservableCollection<RequestHeader> headers)
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

    private void ApplyAuthorization(HttpRequestMessage request, AuthorizationConfig auth)
    {
        if (auth == null || !auth.IsEnabled || auth.AuthType == AuthorizationType.None)
            return;

        var authHeader = auth.GenerateAuthorizationHeader();
        if (!string.IsNullOrEmpty(authHeader))
        {
            if (auth.AuthType == AuthorizationType.ApiKey && auth.ApiKeyLocation == ApiKeyLocation.Header)
                request.Headers.TryAddWithoutValidation(auth.ApiKeyName, auth.ApiKeyValue);
            else
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }
    }

    private static string FormatJsonResponse(string responseText)
    {
        return DevFlow.Serialization.JsonHelper.FormatJson(responseText);
    }
}
