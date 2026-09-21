namespace core.Jobs;

public record RenderJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string ApiKeyId { get; init; } = default!;
    public string? Html { get; init; }
    public string? Url { get; init; }
    public RenderOptions Options { get; init; } = new();
    public int MaxDurationSeconds { get; init; } = 120;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}