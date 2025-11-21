namespace DevFlow.Models;

public record AppConfig
{
    public string? Environment { get; init; }
    public string? ApiBaseUrl { get; init; }
}
