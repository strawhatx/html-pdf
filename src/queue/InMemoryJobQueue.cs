using System.Collections.Concurrent;
using core.Contracts;
using core.Jobs;

namespace queue;

public sealed class InMemoryJobQueue : IJobQueue
{
    private readonly ConcurrentQueue<RenderJob> _jobs = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly ConcurrentDictionary<string, string> _status = new(StringComparer.Ordinal);

    public Task EnqueueAsync(RenderJob job, CancellationToken cancellationToken = default)
    {
        _jobs.Enqueue(job);
        _status[job.Id.ToString()] = "queued";
        _signal.Release();
        return Task.CompletedTask;
    }

    public async Task<RenderJob?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await _signal.WaitAsync(cancellationToken);

        if (_jobs.TryDequeue(out var job))
        {
            _status[job.Id.ToString()] = "dequeued";
            return job;
        }

        return null;
    }

    public Task MarkCompleteAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _status[jobId.ToString()] = "completed";
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid jobId, string reason, CancellationToken cancellationToken = default)
    {
        _status[jobId.ToString()] = $"failed:{reason}";
        return Task.CompletedTask;
    }
}
