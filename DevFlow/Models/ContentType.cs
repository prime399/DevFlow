namespace DevFlow.Models;

public class ContentType
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public ContentType() { }

    public ContentType(string name, string value, string category)
    {
        Name = name;
        Value = value;
        Category = category;
    }

    public override string ToString() => Name;
}

public static class ContentTypes
{
    public static readonly ContentType None = new("None", "", "None");
    
    // Text types
    public static readonly ContentType ApplicationJson = new("application/json", "application/json", "Text");
    public static readonly ContentType ApplicationLdJson = new("application/ld+json", "application/ld+json", "Text");
    public static readonly ContentType ApplicationHalJson = new("application/hal+json", "application/hal+json", "Text");
    public static readonly ContentType ApplicationVndApiJson = new("application/vnd.api+json", "application/vnd.api+json", "Text");
    public static readonly ContentType ApplicationXml = new("application/xml", "application/xml", "Text");
    public static readonly ContentType TextXml = new("text/xml", "text/xml", "Text");
    
    // Structured types
    public static readonly ContentType FormUrlEncoded = new("application/x-www-form-urlencoded", "application/x-www-form-urlencoded", "Structured");
    public static readonly ContentType MultipartFormData = new("multipart/form-data", "multipart/form-data", "Structured");

    public static IReadOnlyList<ContentType> All { get; } = new[]
    {
        None,
        ApplicationJson,
        ApplicationLdJson,
        ApplicationHalJson,
        ApplicationVndApiJson,
        ApplicationXml,
        TextXml,
        FormUrlEncoded,
        MultipartFormData
    };
}
