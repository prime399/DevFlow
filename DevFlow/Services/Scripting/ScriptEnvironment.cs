using System.Collections.Concurrent;

namespace DevFlow.Services.Scripting;

/// <summary>
/// Manages environment variables for pre-request and post-request scripts.
/// </summary>
public class ScriptEnvironment
{
    private readonly ConcurrentDictionary<string, string> _variables = new();

    public static ScriptEnvironment Global { get; } = new();

    public void Set(string key, string value)
    {
        _variables[key] = value;
    }

    public string? Get(string key)
    {
        return _variables.TryGetValue(key, out var value) ? value : null;
    }

    public bool Has(string key)
    {
        return _variables.ContainsKey(key);
    }

    public void Remove(string key)
    {
        _variables.TryRemove(key, out _);
    }

    public void Clear()
    {
        _variables.Clear();
    }

    public IReadOnlyDictionary<string, string> GetAll()
    {
        return _variables;
    }

    /// <summary>
    /// Resolves environment variables in a string.
    /// Variables are in format {{variableName}}
    /// </summary>
    public string ResolveVariables(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = input;
        foreach (var kvp in _variables)
        {
            result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }
        return result;
    }
}
