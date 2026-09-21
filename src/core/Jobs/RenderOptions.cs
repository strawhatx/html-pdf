namespace core.Jobs;

public record RenderOptions
{
    public string PageSize { get; init; } = "A4";
    public string? Margins { get; init; }
    public string? HeaderHtml { get; init; }
    public string? FooterHtml { get; init; }
    public WaitCondition? Wait { get; init; }
}