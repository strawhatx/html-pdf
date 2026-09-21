using core.Contracts;
using core.Jobs;
using data.Entities;
using data.Repositories;
using renderer.Services;
using System.Diagnostics;

namespace renderer;

public sealed class Worker(
    ILogger<Worker> logger,
    IJobQueue jobQueue,
    RenderPipeline renderPipeline,
    IOutputStorage outputStorage,
    IServiceScopeFactory scopeFactory,
    BrowserPool browserPool) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await browserPool.InitializeAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var job = await jobQueue.DequeueAsync(stoppingToken);
            if (job is null)
            {
                await Task.Delay(500, stoppingToken);
                continue;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var jobs = scope.ServiceProvider.GetRequiredService<IJobRepository>();

            try
            {
                await jobs.UpdateStatusAsync(job.Id, JobStatus.Running, ct: stoppingToken);
                var stopwatch = Stopwatch.StartNew();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(30, job.MaxDurationSeconds)));

                var pdfBytes = await renderPipeline.RenderAsync(job, cts.Token);
                await outputStorage.SaveResultAsync(job.Id, pdfBytes, cts.Token);
                stopwatch.Stop();
                var usage = scope.ServiceProvider.GetRequiredService<IUsageRepository>();
                await usage.RecordAsync(new UsageRecord
                {
                    Id = Guid.NewGuid(),
                    ApiKeyId = job.ApiKeyId,
                    JobId = job.Id,
                    ConcurrentSeconds = stopwatch.Elapsed.TotalSeconds,
                    RecordedAt = DateTimeOffset.UtcNow,
                }, cts.Token);
                await jobQueue.MarkCompleteAsync(job.Id, cts.Token);
                await jobs.UpdateStatusAsync(job.Id, JobStatus.Completed, ct: cts.Token);

                logger.LogInformation("Completed job {JobId} ({Bytes} bytes)", job.Id, pdfBytes.Length);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Job {JobId} timed out.", job.Id);
                await jobQueue.MarkFailedAsync(job.Id, "timeout", stoppingToken);
                await jobs.UpdateStatusAsync(job.Id, JobStatus.Failed, "timeout", stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Job {JobId} failed.", job.Id);
                await jobQueue.MarkFailedAsync(job.Id, ex.Message, stoppingToken);
                await jobs.UpdateStatusAsync(job.Id, JobStatus.Failed, ex.Message, stoppingToken);
            }
        }
    }
}
