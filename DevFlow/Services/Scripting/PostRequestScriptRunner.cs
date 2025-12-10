using System.Diagnostics;
using System.Text.Json;
using DevFlow.Models;
using Jint;
using Jint.Native;

namespace DevFlow.Services.Scripting;

/// <summary>
/// Response context passed to post-request scripts
/// </summary>
public class ResponseContext
{
    public int StatusCode { get; set; }
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public long ResponseTime { get; set; }
    public long ResponseSize { get; set; }
}

/// <summary>
/// Runs post-request JavaScript scripts with access to response data.
/// </summary>
public class PostRequestScriptRunner
{
    private readonly ScriptEnvironment _environment;

    public PostRequestScriptRunner(ScriptEnvironment? environment = null)
    {
        _environment = environment ?? ScriptEnvironment.Global;
    }

    public ScriptExecutionResult Execute(string script, ResponseContext? response = null)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return ScriptExecutionResult.Success();
        }

        var result = new ScriptExecutionResult();
        var logs = new List<string>();
        var testResults = new List<TestResult>();
        var totalStopwatch = Stopwatch.StartNew();

        try
        {
            var engine = new Engine(options =>
            {
                options.LimitRecursion(100);
                options.TimeoutInterval(TimeSpan.FromSeconds(5));
                options.MaxStatements(10000);
            });

            // Create pw object with env, test, expect, and response APIs
            var envApi = new EnvironmentApi(_environment);
            var testApi = new TestApi(testResults, logs);
            var responseApi = CreateResponseApi(response);

            engine.SetValue("pw", new
            {
                env = envApi,
                test = new Action<string, Action>(testApi.Test),
                expect = new Func<object?, ExpectChain>((value) => new ExpectChain(value, testResults, logs)),
                response = responseApi
            });

            // Add console.log support
            engine.SetValue("console", new
            {
                log = new Action<object?[]>(args =>
                {
                    var message = string.Join(" ", args.Select(a => a?.ToString() ?? "undefined"));
                    logs.Add(message);
                }),
                warn = new Action<object?[]>(args =>
                {
                    var message = "[WARN] " + string.Join(" ", args.Select(a => a?.ToString() ?? "undefined"));
                    logs.Add(message);
                }),
                error = new Action<object?[]>(args =>
                {
                    var message = "[ERROR] " + string.Join(" ", args.Select(a => a?.ToString() ?? "undefined"));
                    logs.Add(message);
                })
            });

            // Override Date.now()
            engine.SetValue("Date", new
            {
                now = new Func<long>(() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            });

            // Add Math object
            engine.SetValue("Math", new
            {
                random = new Func<double>(() => Random.Shared.NextDouble()),
                floor = new Func<double, double>(Math.Floor),
                ceil = new Func<double, double>(Math.Ceiling),
                round = new Func<double, double>(Math.Round),
                abs = new Func<double, double>(Math.Abs),
                min = new Func<double, double, double>(Math.Min),
                max = new Func<double, double, double>(Math.Max),
                pow = new Func<double, double, double>(Math.Pow),
                sqrt = new Func<double, double>(Math.Sqrt)
            });

            // Execute the script
            engine.Execute(script);

            totalStopwatch.Stop();
            result.IsSuccess = true;
            result.Logs = logs;
            result.TestResults = testResults;
            result.TotalDuration = totalStopwatch.ElapsedMilliseconds;
        }
        catch (Jint.Runtime.JavaScriptException jsEx)
        {
            totalStopwatch.Stop();
            result.IsSuccess = false;
            result.ErrorMessage = $"JavaScript Error: {jsEx.Message}";
            result.ErrorLine = jsEx.Location.Start.Line;
            result.Logs = logs;
            result.TestResults = testResults;
            result.TotalDuration = totalStopwatch.ElapsedMilliseconds;
        }
        catch (TimeoutException)
        {
            totalStopwatch.Stop();
            result.IsSuccess = false;
            result.ErrorMessage = "Script execution timed out (5 second limit)";
            result.Logs = logs;
            result.TestResults = testResults;
            result.TotalDuration = totalStopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            result.IsSuccess = false;
            result.ErrorMessage = $"Error: {ex.Message}";
            result.Logs = logs;
            result.TestResults = testResults;
            result.TotalDuration = totalStopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    private object CreateResponseApi(ResponseContext? response)
    {
        if (response == null)
        {
            return new
            {
                status = 0,
                body = (object?)null,
                headers = new Dictionary<string, string>(),
                time = 0L,
                size = 0L
            };
        }

        // Parse body as JSON if possible
        object? parsedBody = response.Body;
        try
        {
            if (!string.IsNullOrEmpty(response.Body))
            {
                using var doc = JsonDocument.Parse(response.Body);
                parsedBody = ConvertJsonElement(doc.RootElement);
            }
        }
        catch
        {
            // Keep as string if not valid JSON
        }

        return new
        {
            status = response.StatusCode,
            body = parsedBody,
            headers = response.Headers,
            time = response.ResponseTime,
            size = response.ResponseSize
        };
    }

    private object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonElement).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}
