using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevFlow.Shared;
using Supabase.Gotrue;

namespace DevFlow.Serialization;

/// <summary>
/// JSON serialization context for AOT/WASM compatibility.
/// Source generators are required because reflection-based serialization is disabled in WASM.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, string?>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonDocument))]
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(DataItem))]
[JsonSerializable(typeof(IEnumerable<DataItem>))]
[JsonSerializable(typeof(List<DataItem>))]
[JsonSerializable(typeof(DataItem[]))]
public partial class DevFlowJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Helper class for JSON serialization in AOT/WASM environments.
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions s_indentedOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    
    private static readonly JsonSerializerOptions s_indentedSimpleOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Format a JSON string with proper indentation (safe for AOT/WASM).
    /// Uses JsonDocument.WriteTo which doesn't require reflection.
    /// </summary>
    public static string FormatJson(string json, bool relaxedEscaping = true)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        var trimmed = json.TrimStart();
        if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
            return json;

        try
        {
            using var doc = JsonDocument.Parse(json);
            using var stream = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions 
            { 
                Indented = true,
                Encoder = relaxedEscaping 
                    ? System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
                    : null
            });
            doc.WriteTo(writer);
            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return json;
        }
    }

    /// <summary>
    /// Serialize a dictionary to JSON (safe for AOT/WASM).
    /// </summary>
    public static string SerializeDictionary(Dictionary<string, object?> dict)
    {
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });
        
        writer.WriteStartObject();
        foreach (var kvp in dict)
        {
            writer.WritePropertyName(kvp.Key);
            WriteValue(writer, kvp.Value);
        }
        writer.WriteEndObject();
        writer.Flush();
        
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case JsonElement je:
                je.WriteTo(writer);
                break;
            case Dictionary<string, object?> dict:
                writer.WriteStartObject();
                foreach (var kvp in dict)
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteValue(writer, kvp.Value);
                }
                writer.WriteEndObject();
                break;
            case IEnumerable<object?> arr:
                writer.WriteStartArray();
                foreach (var item in arr)
                {
                    WriteValue(writer, item);
                }
                writer.WriteEndArray();
                break;
            default:
                // Fallback: write as string
                writer.WriteStringValue(value.ToString());
                break;
        }
    }

    /// <summary>
    /// Parse JSON variables string into a JsonElement for GraphQL requests.
    /// </summary>
    public static JsonElement? ParseVariables(string variables)
    {
        if (string.IsNullOrWhiteSpace(variables) || variables == "{}")
            return null;

        try
        {
            using var doc = JsonDocument.Parse(variables);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
