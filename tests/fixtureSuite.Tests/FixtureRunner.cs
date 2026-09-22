using Microsoft.Playwright;

namespace fixtureSuite.Tests;

public static class FixtureRunner
{
    public static async Task<byte[]> RunAsync(IBrowser browser, string fixtureFileName)
    {
        var htmlPath = Path.Combine(AppContext.BaseDirectory, "fixtures", fixtureFileName);
        var bundlePath = Path.Combine(AppContext.BaseDirectory, "Assets", "pagedjs", "paged.v0.1.0.js");
        var html = await File.ReadAllTextAsync(htmlPath);

        var page = await browser.NewPageAsync();
        try
        {
            await page.SetContentAsync(html);
            await page.AddScriptTagAsync(new() { Path = bundlePath });
            await page.EvaluateAsync("() => new Paged.Previewer().preview()");
            return await page.PdfAsync(new() { PrintBackground = true, Format = "A4" });
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public static async Task<(byte[] PdfBytes, int PagedJsPageCount)> RunWithPagedJsDiagnosticsAsync(
        IBrowser browser, string fixtureFileName)
    {
        var htmlPath = Path.Combine(AppContext.BaseDirectory, "fixtures", fixtureFileName);
        var bundlePath = Path.Combine(AppContext.BaseDirectory, "Assets", "pagedjs", "paged.v0.1.0.js");
        var html = await File.ReadAllTextAsync(htmlPath);

        var page = await browser.NewPageAsync();
        try
        {
            await page.SetContentAsync(html);
            await page.AddScriptTagAsync(new() { Path = bundlePath });
            await page.EvaluateAsync("() => new Paged.Previewer().preview()");
            var pagedJsPageCount = await page.EvaluateAsync<int>(
                "() => document.querySelectorAll('.pagedjs_page').length");
            var pdfBytes = await page.PdfAsync(new() { PrintBackground = true, Format = "A4" });
            return (pdfBytes, pagedJsPageCount);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public static async Task<byte[]> RunWithoutPagedJsAsync(IBrowser browser, string fixtureFileName)
    {
        var htmlPath = Path.Combine(AppContext.BaseDirectory, "fixtures", fixtureFileName);
        var html = await File.ReadAllTextAsync(htmlPath);

        var page = await browser.NewPageAsync();
        try
        {
            await page.SetContentAsync(html);
            return await page.PdfAsync(new() { PrintBackground = true, Format = "A4" });
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}