using core.Jobs;
using data.Entities;

namespace data.Repositories;

public interface IJobRepository
{
    /// <summary>
    /// Creates a new job record in the database for the given render job.
    /// </summary>
    /// <param name="job">The render job to create a record for.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The created job record.</returns>
    Task<JobRecord> CreateAsync(RenderJob job, CancellationToken ct = default);
    
    /// <summary>
    /// Updates the status of a job record in the database.
    /// </summary>
    /// <param name="jobId">The ID of the job to update.</param>
    /// <param name="status">The new status for the job.</param>
    /// <param name="error">An optional error message for the job.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    Task UpdateStatusAsync(Guid jobId, JobStatus status, string? error = null, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves a job record from the database by its ID.
    /// </summary>
    /// <param name="jobId">The ID of the job to retrieve.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The retrieved job record, or null if not found.</returns>
    Task<JobRecord?> GetAsync(Guid jobId, CancellationToken ct = default);
}