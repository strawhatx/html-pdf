using core.Jobs;

namespace core.Contracts;

public interface IJobQueue
{
    Task EnqueueAsync(RenderJob job, CancellationToken ct = default);
    Task<RenderJob?> DequeueAsync(CancellationToken ct = default);
    Task MarkCompleteAsync(Guid jobId, CancellationToken ct = default);
    Task MarkFailedAsync(Guid jobId, string reason, CancellationToken ct = default);
}