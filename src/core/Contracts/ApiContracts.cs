namespace core;

public sealed class ConvertRequest
{
    public string? Html { get; set; }
    public string? Url { get; set; }
    public ConvertOptions? Options { get; set; }
}

public sealed class ConvertOptions
{
    public string PageSize { get; set; } = "A4";
    public MarginOptions Margins { get; set; } = new();
    public string? Header { get; set; }
    public string? Footer { get; set; }
    public WaitOptions? Wait { get; set; }
}

public sealed class MarginOptions
{
    public decimal Top { get; set; } = 10m;
    public decimal Right { get; set; } = 10m;
    public decimal Bottom { get; set; } = 10m;
    public decimal Left { get; set; } = 10m;
}

public sealed class WaitOptions
{
    public string Type { get; set; } = "timeout";
    public string? Value { get; set; }
    public int Ms { get; set; }
}

public sealed record JobRecord(string Id, string Status, long PayloadSize, DateTimeOffset CreatedAt);

public sealed record UsageRecord(string Key, long JobCount, long BytesProcessed);
