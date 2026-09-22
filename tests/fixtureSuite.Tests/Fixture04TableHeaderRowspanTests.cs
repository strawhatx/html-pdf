using UglyToad.PdfPig;

namespace fixtureSuite.Tests;

[Collection("Playwright")]
public sealed class Fixture04TableHeaderRowspanTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task WithPagedJs_RowspanCellStraddlingBreak_DuplicatesOnContinuationPage()
    {
        var (pdfBytes, pagedJsPageCount) = await FixtureRunner.RunWithPagedJsDiagnosticsAsync(
            fixture.Browser, "04-table-header-rowspan.html");

        Assert.True(pagedJsPageCount > 1,
            "paged.js produced no page-chunking DOM structure (.pagedjs_page) - " +
            "pagination may not have run, so this result can't be attributed to paged.js.");

        using var document = PdfDocument.Open(new MemoryStream(pdfBytes));
        var pages = PagesContainingWord(document, "D");

        Assert.True(pages.Count >= 2,
            "Group D's rowspan cell did not straddle a page break in this render - " +
            "the fixture didn't produce a break at the point needed to test rowspan duplication.");
    }

    [Fact]
    public async Task WithoutPagedJs_RowspanCellStraddlingBreak_LosesGroupLabelOnContinuationPage()
    {
        var pdfBytes = await FixtureRunner.RunWithoutPagedJsAsync(fixture.Browser, "04-table-header-rowspan.html");
        using var document = PdfDocument.Open(new MemoryStream(pdfBytes));

        var pages = PagesContainingWord(document, "D");

        Assert.True(pages.Count == 1,
            "Raw Chromium printing carried the rowspan cell's value onto the continuation page on its own, " +
            "meaning this fixture no longer proves paged.js is doing the work of preserving rowspan data across breaks.");
    }

    private static List<int> PagesContainingWord(PdfDocument document, string word)
    {
        return document.GetPages()
            .Where(page => page.GetWords().Any(w => w.Text == word))
            .Select(page => page.Number)
            .ToList();
    }
}
