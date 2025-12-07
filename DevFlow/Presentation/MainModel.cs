using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using DevFlow.Models;

namespace DevFlow.Presentation;

public partial record MainModel
{
    private readonly INavigator _navigator;
    private readonly IHttpClientFactory _httpClientFactory;

    public MainModel(
        IStringLocalizer localizer,
        IOptions<AppConfig> appInfo,
        INavigator navigator,
        IHttpClientFactory httpClientFactory)
    {
        _navigator = navigator;
        _httpClientFactory = httpClientFactory;
        Title = "DevFlow";
        Title += $" - {localizer["ApplicationName"]}";
        Title += $" - {appInfo?.Value?.Environment}";
    }

    public string? Title { get; }

    public IReadOnlyList<string> Methods { get; } = new[] { "GET", "POST", "PUT", "PATCH", "DELETE" };

    public IState<string> SelectedMethod => State<string>.Value(this, () => "GET");
    public IState<string> RequestUrl => State<string>.Value(this, () => "https://echo.hoppscotch.io");
    public IState<string> HeadersText => State<string>.Value(this, () => "accept: application/json");
    public IState<string> BodyText => State<string>.Value(this, () => "{\n  \"hello\": \"world\"\n}");

    public ObservableCollection<RequestParameter> Parameters { get; } = new()
    {
        new RequestParameter("", "", "", true)
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

    private string BuildQueryParamsFromCollection()
    {
        var enabledParams = Parameters.Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.ParamKey));
        return string.Join("\n", enabledParams.Select(p => $"{p.ParamKey}:{p.ParamValue}"));
    }

    public IState<string> ResponseStatus => State<string>.Value(this, () => "Awaiting request");
    public IState<string> ResponseMeta => State<string>.Value(this, () => string.Empty);
    public IState<string> ResponseBody => State<string>.Value(this, () => string.Empty);
    public IState<string> ErrorMessage => State<string>.Value(this, () => string.Empty);
    public IState<bool> IsSending => State<bool>.Value(this, () => false);

    public async ValueTask SendRequest(CancellationToken ct)
    {
        if (await IsSending)
        {
            return;
        }

        var urlInput = await RequestUrl;
        var methodName = await SelectedMethod ?? HttpMethod.Get.Method;
        var queryText = BuildQueryParamsFromCollection();
        var headersRaw = await HeadersText ?? string.Empty;
        var bodyText = await BodyText ?? string.Empty;

        if (string.IsNullOrWhiteSpace(urlInput))
        {
            await ErrorMessage.UpdateAsync(_ => "Enter a target URL to send the request.", ct);
            return;
        }

        await ErrorMessage.UpdateAsync(_ => string.Empty, ct);
        await IsSending.UpdateAsync(_ => true, ct);

        try
        {
            var requestUri = BuildUri(urlInput, queryText);
            using var request = new HttpRequestMessage(new HttpMethod(methodName), requestUri);
            AddHeaders(request, headersRaw);

            if (AllowsBody(methodName) && !string.IsNullOrWhiteSpace(bodyText))
            {
                request.Content = new StringContent(bodyText, Encoding.UTF8, "application/json");
            }

            var httpClient = _httpClientFactory.CreateClient("ApiTester");
            var stopwatch = Stopwatch.StartNew();
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            var responseBytes = await response.Content.ReadAsByteArrayAsync(ct);
            stopwatch.Stop();

            var responseText = await response.Content.ReadAsStringAsync(ct);

            await ResponseStatus.UpdateAsync(_ => $"{(int)response.StatusCode} {response.ReasonPhrase}", ct);
            await ResponseMeta.UpdateAsync(_ => $"Time: {stopwatch.ElapsedMilliseconds} ms    Size: {(responseBytes.Length / 1024d):0.00} KB", ct);
            await ResponseBody.UpdateAsync(_ => responseText, ct);
        }
        catch (Exception ex)
        {
            await ErrorMessage.UpdateAsync(_ => ex.Message, ct);
            await ResponseStatus.UpdateAsync(_ => "Request failed", ct);
            await ResponseMeta.UpdateAsync(_ => string.Empty, ct);
            await ResponseBody.UpdateAsync(_ => string.Empty, ct);
        }
        finally
        {
            await IsSending.UpdateAsync(_ => false, ct);
        }
    }

    public async ValueTask ResetResponse(CancellationToken ct)
    {
        await ResponseStatus.UpdateAsync(_ => "Awaiting request", ct);
        await ResponseMeta.UpdateAsync(_ => string.Empty, ct);
        await ResponseBody.UpdateAsync(_ => string.Empty, ct);
        await ErrorMessage.UpdateAsync(_ => string.Empty, ct);
    }

    private static Uri BuildUri(string urlInput, string queryText)
    {
        var builder = new UriBuilder(urlInput);
        var incomingQuery = BuildQueryString(queryText);
        if (!string.IsNullOrWhiteSpace(incomingQuery))
        {
            var existing = builder.Query.TrimStart('?');
            builder.Query = string.IsNullOrWhiteSpace(existing)
                ? incomingQuery
                : $"{existing}&{incomingQuery}";
        }

        return builder.Uri;
    }

    private static string BuildQueryString(string queryText)
    {
        var pairs = ParseKeyValuePairs(queryText);
        var encoded = pairs
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}")
            .ToArray();
        return string.Join("&", encoded);
    }

    private static void AddHeaders(HttpRequestMessage request, string rawHeaders)
    {
        foreach (var (key, value) in ParseKeyValuePairs(rawHeaders))
        {
            if (!request.Headers.TryAddWithoutValidation(key, value))
            {
                request.Content ??= new StringContent(string.Empty);
                _ = request.Content.Headers.TryAddWithoutValidation(key, value);
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
