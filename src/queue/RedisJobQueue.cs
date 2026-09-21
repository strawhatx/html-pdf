using System.Text.Json;
using core.Contracts;
using core.Jobs;
using StackExchange.Redis;

namespace queue;

public sealed class RedisJobQueue : IJobQueue
{
    private const string DequeueScript = """
        local jobId = redis.call('RPOP', KEYS[1])
        if not jobId then return false end
        redis.call('ZADD', KEYS[2], ARGV[1], jobId)
        return jobId
        """;

    private readonly IDatabase _db;
    private readonly QueueOptions _options;

    public RedisJobQueue(RedisConnectionFactory factory, QueueOptions options)
    {
        _db = factory.GetDatabase();
        _options = options;
    }

    public async Task EnqueueAsync(RenderJob job, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var jobId = job.Id.ToString("N");
        var payload = JsonSerializer.Serialize(job);

        await _db.HashSetAsync(_options.PayloadHashKey, jobId, payload);
        await _db.ListLeftPushAsync(_options.PendingListKey, jobId);
    }

    public async Task<RenderJob?> DequeueAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var jobId = await _db.ScriptEvaluateAsync(
            DequeueScript,
            [_options.PendingListKey, _options.ProcessingSetKey],
            [DateTimeOffset.UtcNow.ToUnixTimeSeconds()]);

        if (jobId.IsNull)
        {
            return null;
        }

        var payload = await _db.HashGetAsync(_options.PayloadHashKey, (RedisValue)jobId);
        return payload.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<RenderJob>(payload.ToString());
    }

    public async Task MarkCompleteAsync(Guid jobId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var id = jobId.ToString("N");
        await _db.SortedSetRemoveAsync(_options.ProcessingSetKey, id);
        await _db.HashDeleteAsync(_options.PayloadHashKey, id);
    }

    public async Task MarkFailedAsync(Guid jobId, string reason, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var id = jobId.ToString("N");
        await _db.SortedSetRemoveAsync(_options.ProcessingSetKey, id);
        await _db.HashDeleteAsync(_options.PayloadHashKey, id);
    }

    public async Task<int> RequeueExpiredAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-_options.VisibilityTimeoutSeconds).ToUnixTimeSeconds();
        var expired = await _db.SortedSetRangeByScoreAsync(_options.ProcessingSetKey, 0, cutoff);

        foreach (var jobId in expired)
        {
            ct.ThrowIfCancellationRequested();
            await _db.SortedSetRemoveAsync(_options.ProcessingSetKey, jobId);
            await _db.ListLeftPushAsync(_options.PendingListKey, jobId);
        }

        return expired.Length;
    }
}