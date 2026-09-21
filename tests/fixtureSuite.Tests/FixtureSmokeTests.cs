using UglyToad.PdfPig;

namespace fixtureSuite.Tests;

 [Collection("Playwright")]
public sealed class FixtureSmokeTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task GeneratedPdfContainsExpectedSmokeFixtureText()
    {
        var pdfBytes = await FixtureRunner.RunAsync(fixture.Browser, "hello-world.html");
        using var document = PdfDocument.Open(new MemoryStream(pdfBytes));
        var text = string.Join("\n", document.GetPages().Select(page => page.Text));

        Assert.Contains("Hello", text);
        Assert.Contains("Smoke test paragraph.", text);
    }
}