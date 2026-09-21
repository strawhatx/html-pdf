using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using queue;

namespace renderer;

public sealed class RedisVisibilitySweepService(
    RedisJobQueue jobQueue,
    QueueOptions options,
    ILogger<RedisVisibilitySweepService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Max(1, Math.Min(options.VisibilityTimeoutSeconds / 2, 30));
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var requeued = await jobQueue.RequeueExpiredAsync(stoppingToken);
                if (requeued > 0)
                {
                    logger.LogWarning("Requeued {Count} expired Redis jobs.", requeued);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Redis visibility sweep failed.");
            }
        }
    }
}