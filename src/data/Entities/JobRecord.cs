using core.Jobs;

namespace data.Entities;

public class JobRecord
{
    public Guid Id { get; set; }
    public string ApiKeyId { get; set; } = default!;
    public JobStatus Status { get; set; }
    public string? ResultUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}