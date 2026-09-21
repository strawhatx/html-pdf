using System.Data;
using core;
using Dapper;

namespace data;

public sealed class JobStore : RepositoryBase
{
    private readonly Dictionary<string, JobRecord> _fallbackJobs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UsageRecord> _fallbackUsage = new(StringComparer.Ordinal);

    public JobStore(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public string CreateJob(string status, long payloadSize)
    {
        var id = Guid.NewGuid().ToString("N");

        try
        {
            using var connection = OpenWriteConnection();
            connection.Execute(
                "INSERT INTO jobs (id, status, payload_size, created_at) VALUES (@Id, @Status, @PayloadSize, @CreatedAt)",
                new
                {
                    Id = id,
                    Status = status,
                    PayloadSize = payloadSize,
                    CreatedAt = DateTimeOffset.UtcNow
                });

            return id;
        }
        catch
        {
            var record = new JobRecord(id, status, payloadSize, DateTimeOffset.UtcNow);
            _fallbackJobs[id] = record;
            return id;
        }
    }

    public JobRecord? Get(string id)
    {
        try
        {
            using var connection = OpenReadConnection();
            var record = connection.QuerySingleOrDefault<JobRecord>(
                "SELECT id AS Id, status AS Status, payload_size AS PayloadSize, created_at AS CreatedAt FROM jobs WHERE id = @Id",
                new { Id = id });
            return record;
        }
        catch
        {
            return _fallbackJobs.TryGetValue(id, out var fallback) ? fallback : null;
        }
    }

    public object UsageFor(string key)
    {
        try
        {
            using var connection = OpenReadConnection();
            var record = connection.QuerySingleOrDefault<UsageRecord>(
                "SELECT api_key AS Key, job_count AS JobCount, bytes_processed AS BytesProcessed FROM usage WHERE api_key = @Key",
                new { Key = key });

            return new { key, jobs = record?.JobCount ?? 0, bytes = record?.BytesProcessed ?? 0 };
        }
        catch
        {
            if (!_fallbackUsage.TryGetValue(key, out var usage))
            {
                usage = new UsageRecord(key, 0, 0);
                _fallbackUsage[key] = usage;
            }

            return new { key, jobs = usage.JobCount, bytes = usage.BytesProcessed };
        }
    }
}
