using data.Entities;
using Microsoft.EntityFrameworkCore;

namespace data.Repositories;

public sealed class UsageRepository(ApiDbContext db) : IUsageRepository
{
     ///<inheritdoc />
    public async Task<UsageRecord> RecordAsync(UsageRecord usage, CancellationToken ct = default)
    {
        db.UsageRecords.Add(usage);
        await db.SaveChangesAsync(ct);
        return usage;
    }

 ///<inheritdoc />
    public async Task<double> TotalConcurrentSecondsAsync(string apiKeyId, CancellationToken ct = default) =>
        await db.UsageRecords
            .Where(usage => usage.ApiKeyId == apiKeyId)
            .SumAsync(usage => (double?)usage.ConcurrentSeconds, ct) ?? 0d;
}