using System.Diagnostics;
using DevFlow.Models;
using Jint;
using Jint.Native;

namespace DevFlow.Services.Scripting;

/// <summary>
/// Runs pre-request JavaScript scripts using the Jint interpreter.
/// Provides a 'pw' object similar to Hoppscotch for environment variable manipulation and testing.
/// </summary>
public class PreRequestScriptRunner
{
    private readonly ScriptEnvironment _environment;

    public PreRequestScriptRunner(ScriptEnvironment? environment = null)
    {
        _environment = environment ?? ScriptEnvironment.Global;
    }

    public ScriptExecutionResult Execute(string script)
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

            // Create pw object with env and test APIs
            var envApi = new EnvironmentApi(_environment);
            var testApi = new TestApi(testResults, logs);
            var expectApi = new ExpectApi(testResults, logs);
            
            engine.SetValue("pw", new 
            { 
                env = envApi,
                test = new Action<string, Action>(testApi.Test),
                expect = new Func<object?, ExpectChain>((value) => new ExpectChain(value, testResults, logs))
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
}

/// <summary>
/// Test API exposed as pw.test() in scripts
/// </summary>
public class TestApi
{
    private readonly List<TestResult> _results;
    private readonly List<string> _logs;

    public TestApi(List<TestResult> results, List<string> logs)
    {
        _results = results;
        _logs = logs;
    }

    public void Test(string name, Action testFn)
    {
        var sw = Stopwatch.StartNew();
        var result = new TestResult { Name = name };

        try
        {
            testFn();
            sw.Stop();
            result.Status = TestStatus.Passed;
            result.Duration = sw.ElapsedMilliseconds;
            result.Message = "Test passed";
        }
        catch (AssertionException ex)
        {
            sw.Stop();
            result.Status = TestStatus.Failed;
            result.Duration = sw.ElapsedMilliseconds;
            result.Message = ex.Message;
            result.Expected = ex.Expected;
            result.Actual = ex.Actual;
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Status = TestStatus.Failed;
            result.Duration = sw.ElapsedMilliseconds;
            result.Message = $"Error: {ex.Message}";
        }

        _results.Add(result);
    }
}

/// <summary>
/// Expect API for fluent assertions
/// </summary>
public class ExpectApi
{
    private readonly List<TestResult> _results;
    private readonly List<string> _logs;

    public ExpectApi(List<TestResult> results, List<string> logs)
    {
        _results = results;
        _logs = logs;
    }
}

/// <summary>
/// Fluent assertion chain
/// </summary>
public class ExpectChain
{
    private readonly object? _actual;
    private readonly List<TestResult> _results;
    private readonly List<string> _logs;
    private bool _negated;

    public ExpectChain(object? actual, List<TestResult> results, List<string> logs)
    {
        _actual = actual;
        _results = results;
        _logs = logs;
    }

    public ExpectChain not
    {
        get
        {
            _negated = !_negated;
            return this;
        }
    }

    public ExpectChain toBe(object? expected)
    {
        var areEqual = Equals(_actual, expected);
        if (_negated) areEqual = !areEqual;

        if (!areEqual)
        {
            throw new AssertionException(
                _negated ? $"Expected value to not be {expected}" : $"Expected {expected} but got {_actual}",
                expected?.ToString() ?? "null",
                _actual?.ToString() ?? "null"
            );
        }
        return this;
    }

    public ExpectChain toEqual(object? expected)
    {
        return toBe(expected);
    }

    public ExpectChain toBeTruthy()
    {
        var isTruthy = _actual != null && !Equals(_actual, false) && !Equals(_actual, 0) && !Equals(_actual, "");
        if (_negated) isTruthy = !isTruthy;

        if (!isTruthy)
        {
            throw new AssertionException(
                _negated ? "Expected value to be falsy" : "Expected value to be truthy",
                _negated ? "falsy" : "truthy",
                _actual?.ToString() ?? "null"
            );
        }
        return this;
    }

    public ExpectChain toBeFalsy()
    {
        var isFalsy = _actual == null || Equals(_actual, false) || Equals(_actual, 0) || Equals(_actual, "");
        if (_negated) isFalsy = !isFalsy;

        if (!isFalsy)
        {
            throw new AssertionException(
                _negated ? "Expected value to be truthy" : "Expected value to be falsy",
                _negated ? "truthy" : "falsy",
                _actual?.ToString() ?? "null"
            );
        }
        return this;
    }

    public ExpectChain toBeNull()
    {
        var isNull = _actual == null;
        if (_negated) isNull = !isNull;

        if (!isNull)
        {
            throw new AssertionException(
                _negated ? "Expected value to not be null" : "Expected null",
                _negated ? "not null" : "null",
                _actual?.ToString() ?? "null"
            );
        }
        return this;
    }

    public ExpectChain toContain(object? expected)
    {
        var contains = _actual?.ToString()?.Contains(expected?.ToString() ?? "") ?? false;
        if (_negated) contains = !contains;

        if (!contains)
        {
            throw new AssertionException(
                _negated ? $"Expected value to not contain {expected}" : $"Expected value to contain {expected}",
                expected?.ToString() ?? "",
                _actual?.ToString() ?? "null"
            );
        }
        return this;
    }

    public ExpectChain toBeGreaterThan(double expected)
    {
        var actual = Convert.ToDouble(_actual);
        var isGreater = actual > expected;
        if (_negated) isGreater = !isGreater;

        if (!isGreater)
        {
            throw new AssertionException(
                _negated ? $"Expected {actual} to not be greater than {expected}" : $"Expected {actual} to be greater than {expected}",
                $"> {expected}",
                actual.ToString()
            );
        }
        return this;
    }

    public ExpectChain toBeLessThan(double expected)
    {
        var actual = Convert.ToDouble(_actual);
        var isLess = actual < expected;
        if (_negated) isLess = !isLess;

        if (!isLess)
        {
            throw new AssertionException(
                _negated ? $"Expected {actual} to not be less than {expected}" : $"Expected {actual} to be less than {expected}",
                $"< {expected}",
                actual.ToString()
            );
        }
        return this;
    }

    public ExpectChain toHaveLength(int expected)
    {
        int actual = 0;
        if (_actual is string s) actual = s.Length;
        else if (_actual is System.Collections.ICollection c) actual = c.Count;

        var hasLength = actual == expected;
        if (_negated) hasLength = !hasLength;

        if (!hasLength)
        {
            throw new AssertionException(
                _negated ? $"Expected length to not be {expected}" : $"Expected length {expected} but got {actual}",
                expected.ToString(),
                actual.ToString()
            );
        }
        return this;
    }
}

public class AssertionException : Exception
{
    public string Expected { get; }
    public string Actual { get; }

    public AssertionException(string message, string expected, string actual) : base(message)
    {
        Expected = expected;
        Actual = actual;
    }
}

/// <summary>
/// API exposed as pw.env in scripts
/// </summary>
public class EnvironmentApi
{
    private readonly ScriptEnvironment _environment;

    public EnvironmentApi(ScriptEnvironment environment)
    {
        _environment = environment;
    }

    public void set(string key, object? value)
    {
        _environment.Set(key, value?.ToString() ?? "");
    }

    public string? get(string key)
    {
        return _environment.Get(key);
    }

    public bool has(string key)
    {
        return _environment.Has(key);
    }

    public void delete(string key)
    {
        _environment.Remove(key);
    }
}

public class ScriptExecutionResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int ErrorLine { get; set; }
    public List<string> Logs { get; set; } = new();
    public List<TestResult> TestResults { get; set; } = new();
    public long TotalDuration { get; set; }

    public int PassedCount => TestResults.Count(t => t.Status == TestStatus.Passed);
    public int FailedCount => TestResults.Count(t => t.Status == TestStatus.Failed);
    public int SkippedCount => TestResults.Count(t => t.Status == TestStatus.Skipped);
    public bool HasTests => TestResults.Count > 0;
    public bool AllPassed => HasTests && FailedCount == 0;

    public static ScriptExecutionResult Success() => new() { IsSuccess = true };
}
