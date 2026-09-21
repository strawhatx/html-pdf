using Microsoft.Playwright;
using UglyToad.PdfPig;

namespace fixtureSuite.Tests;

public sealed class FixtureSmokeTests
{
    [Fact]
    public async Task GeneratedPdfContainsExpectedSmokeFixtureText()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "hello-world.html");
        var bundlePath = Path.Combine(AppContext.BaseDirectory, "Assets", "pagedjs", "paged.v0.1.0.js");
        var html = await File.ReadAllTextAsync(fixturePath);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        await page.AddScriptTagAsync(new() { Path = bundlePath });
        await page.EvaluateAsync("() => new Paged.Previewer().preview()");

        var pdfBytes = await page.PdfAsync(new() { PrintBackground = true, Format = "A4" });
        using var document = PdfDocument.Open(new MemoryStream(pdfBytes));
        var text = string.Join("\n", document.GetPages().Select(page => page.Text));

        Assert.Contains("Hello", text);
        Assert.Contains("Smoke test paragraph.", text);
    }
}