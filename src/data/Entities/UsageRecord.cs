namespace data.Entities;

public class UsageRecord
{
    public Guid Id { get; set; }
    public string ApiKeyId { get; set; } = default!;
    public Guid JobId { get; set; }
    public double ConcurrentSeconds { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}