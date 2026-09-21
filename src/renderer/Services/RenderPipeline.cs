using Microsoft.Playwright;
using System.Text.Json;
using core.Jobs;
using renderer;

namespace renderer.Services;

public sealed class RenderPipeline(
    BrowserPool browserPool,
    SsrfValidator ssrfValidator)
{
    public async Task<byte[]> RenderAsync(RenderJob job, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(job.Url))
        {
            await ssrfValidator.ValidateAsync(job.Url, cancellationToken);
        }

        await using var rentedContext = await browserPool.AcquireContextAsync(cancellationToken);
        var page = await rentedContext.Context.NewPageAsync();

        if (!string.IsNullOrWhiteSpace(job.Url))
        {
            await page.GotoAsync(job.Url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });
        }
        else
        {
            await page.SetContentAsync(job.Html ?? "<html><body></body></html>");
        }

        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Assets", "pagedjs", "pagedjs.bundle.v0.1.0.js");
        if (File.Exists(scriptPath))
        {
            await page.AddScriptTagAsync(new() { Path = scriptPath });
        }

        await WaitForReadyAsync(page, job.Options.Wait, cancellationToken);

        var pdf = await page.PdfAsync(new PagePdfOptions
        {
            PrintBackground = true,
            PreferCSSPageSize = true,
        });

        return pdf;
    }

    private static async Task WaitForReadyAsync(IPage page, WaitCondition? waitCondition, CancellationToken cancellationToken)
    {
        if (waitCondition is null)
        {
            return;
        }

        var timeout = waitCondition.TimeoutMs ?? 30000;
        switch (waitCondition.Type)
        {
            case WaitType.Signal:
                if (!string.IsNullOrWhiteSpace(waitCondition.Value))
                {
                    var variableName = JsonSerializer.Serialize(waitCondition.Value);
                    await page.WaitForFunctionAsync(
                        $"() => Boolean(window[{variableName}])",
                        null,
                        new PageWaitForFunctionOptions { Timeout = timeout });
                }
                break;
            case WaitType.Selector:
                if (!string.IsNullOrWhiteSpace(waitCondition.Value))
                {
                    await page.WaitForSelectorAsync(waitCondition.Value, new() { Timeout = timeout });
                }
                break;
            case WaitType.Timeout:
                await Task.Delay(waitCondition.TimeoutMs ?? 0, cancellationToken);
                break;
        }
    }
}
