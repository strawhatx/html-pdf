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

    /// <summary>
    /// Dequeues a job from the in-memory queue.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The dequeued render job, or null if the queue is empty.</returns
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

    /// <summary>
    /// Gets the status of a job in the queue.
    /// </summary>
    /// <param name="jobId">The ID of the job to get the status for.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The status of the job, or null if the job is not
    public Task MarkCompleteAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _status[jobId.ToString()] = "completed";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Marks a job as failed in the queue.
    /// </summary>
    /// <param name="jobId">The ID of the job to mark as failed.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task MarkFailedAsync(Guid jobId, string reason, CancellationToken cancellationToken = default)
    {
        _status[jobId.ToString()] = $"failed:{reason}";
        return Task.CompletedTask;
    }
}
