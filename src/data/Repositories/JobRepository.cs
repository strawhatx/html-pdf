using core.Jobs;
using data.Entities;
using Microsoft.EntityFrameworkCore;

namespace data.Repositories;

public sealed class JobRepository(ApiDbContext db) : IJobRepository
{
    ///<inheritdoc />
    public async Task<JobRecord> CreateAsync(RenderJob job, CancellationToken ct = default)
    {
        var record = new JobRecord
        {
            Id = job.Id,
            ApiKeyId = job.ApiKeyId,
            Status = JobStatus.Pending,
            CreatedAt = job.CreatedAt,
        };

        db.Jobs.Add(record);
        await db.SaveChangesAsync(ct);
        return record;
    }
    ///<inheritdoc />
    public async Task UpdateStatusAsync(Guid jobId, JobStatus status, string? error = null, CancellationToken ct = default)
    {
        var record = await db.Jobs.SingleOrDefaultAsync(job => job.Id == jobId, ct)
            ?? throw new KeyNotFoundException($"Job '{jobId}' was not found.");

        record.Status = status;
        record.ErrorMessage = error;
        record.CompletedAt = status is JobStatus.Completed or JobStatus.Failed
            ? DateTimeOffset.UtcNow
            : null;

        await db.SaveChangesAsync(ct);
    }
    
    ///<inheritdoc />
    public Task<JobRecord?> GetAsync(Guid jobId, CancellationToken ct = default) =>
        db.Jobs.AsNoTracking().SingleOrDefaultAsync(job => job.Id == jobId, ct);
}