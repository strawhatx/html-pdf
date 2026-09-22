using UglyToad.PdfPig;

namespace fixtureSuite.Tests;

[Collection("Playwright")]
public sealed class Fixture01NestedTableBreakInsideAvoidTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task WithPagedJs_CardWithBreakInsideAvoid_NeverSplitsAcrossPages()
    {
        var (pdfBytes, pagedJsPageCount) = await FixtureRunner.RunWithPagedJsDiagnosticsAsync(
            fixture.Browser, "01-nested-table-break-inside-avoid.html");

        Assert.True(pagedJsPageCount > 1,
            "paged.js produced no page-chunking DOM structure (.pagedjs_page) - " +
            "pagination may not have run, so this result can't be attributed to paged.js.");

        using var document = PdfDocument.Open(new MemoryStream(pdfBytes));
        AssertCardIsWhollyOnOnePage(document);
    }

    [Fact]
    public async Task WithoutPagedJs_RawChromiumPrint_AlsoHonorsBreakInsideAvoid()
    {
        // Parity result: raw Chromium print already honors break-inside: avoid on this fixture
        // (confirmed by a probe with the property stripped, which does split the card 7/1 across
        // the break), so this can't be a negative control proving paged.js does extra work here.
        // Kept as a regression guard on the baseline instead - the WithPagedJs test above is still
        // the primary assertion for this fixture.
        var pdfBytes = await FixtureRunner.RunWithoutPagedJsAsync(
            fixture.Browser, "01-nested-table-break-inside-avoid.html");
        using var document = PdfDocument.Open(new MemoryStream(pdfBytes));
        AssertCardIsWhollyOnOnePage(document);
    }

    private static void AssertCardIsWhollyOnOnePage(PdfDocument document)
    {
        var startPage = FindPageContaining(document, "CARDSTART");
        var endPage = FindPageContaining(document, "CARDEND");

        Assert.True(startPage == endPage,
            $"The card started on page {startPage} but ended on page {endPage} - " +
            "break-inside: avoid was violated and the nested table's card was split across a page break.");
    }

    private static int FindPageContaining(PdfDocument document, string phrase)
    {
        foreach (var page in document.GetPages())
        {
            var lines = page.GetWords()
                .GroupBy(word => Math.Round(word.BoundingBox.Bottom, 0))
                .Select(group => string.Join(" ", group.OrderBy(word => word.BoundingBox.Left).Select(word => word.Text)));

            if (lines.Any(line => line.Contains(phrase, StringComparison.Ordinal)))
            {
                return page.Number;
            }
        }

        throw new InvalidOperationException($"Phrase '{phrase}' was not found on any page.");
    }
}
