using data.Entities;

namespace data.Repositories;

public interface IUsageRepository
{
    /// <summary>
    /// Records a new usage entry in the database.
    /// </summary>
    /// <param name="usage">The usage record to be added.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The recorded usage entry.</returns>    
    Task<UsageRecord> RecordAsync(UsageRecord usage, CancellationToken ct = default);
    
    /// <summary>
    /// Calculates the total concurrent seconds used by a specific API key.
    /// </summary>
    /// <param name="apiKeyId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<double> TotalConcurrentSecondsAsync(string apiKeyId, CancellationToken ct = default);
}