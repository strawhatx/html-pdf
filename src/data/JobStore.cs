using core;

namespace data;

public sealed class JobStore
{
    private readonly Dictionary<string, JobRecord> _jobs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UsageRecord> _usage = new(StringComparer.Ordinal);

    public string CreateJob(string status, long payloadSize)
    {
        var id = Guid.NewGuid().ToString("N");
        var record = new JobRecord(id, status, payloadSize, DateTimeOffset.UtcNow);
        _jobs[id] = record;
        return id;
    }

    public JobRecord? Get(string id) => _jobs.TryGetValue(id, out var value) ? value : null;

    public object UsageFor(string key)
    {
        if (!_usage.TryGetValue(key, out var usage))
        {
            usage = new UsageRecord(key, 0, 0);
            _usage[key] = usage;
        }

        return new { key, jobs = usage.JobCount, bytes = usage.BytesProcessed };
    }
}
