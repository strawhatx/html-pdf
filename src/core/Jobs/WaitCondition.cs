namespace core.Jobs;

public record WaitCondition
{
    public WaitType Type { get; init; }
    public string? Value { get; init; }
    public int? TimeoutMs { get; init; }
}

public enum WaitType
{
    Signal,
    Selector,
    Timeout,
}