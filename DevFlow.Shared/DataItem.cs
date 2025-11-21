namespace DevFlow.Shared;

/// <summary>
/// Represents a data item that can be synced across platforms.
/// </summary>
public record DataItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
    public bool IsCompleted { get; init; }
}
