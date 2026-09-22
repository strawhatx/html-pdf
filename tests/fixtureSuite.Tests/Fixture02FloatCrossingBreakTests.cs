using UglyToad.PdfPig;

namespace fixtureSuite.Tests;

[Collection("Playwright")]
public sealed class Fixture02FloatCrossingBreakTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task WithoutPagedJs_TailContentReflowsBesideOngoingMultiPageFloat()
    {
        var pdfBytes = await FixtureRunner.RunWithoutPagedJsAsync(fixture.Browser, "02-float-crossing-break.html");
        using var document = PdfDocument.Open(new MemoryStream(pdfBytes));

        var floatEndPage = FindPageContaining(document, "FLOATEND");
        var firstTailPage = FindPageContaining(document, "Tail paragraph 1.");

        Assert.True(floatEndPage > 1,
            "The float didn't actually straddle a page break in this render - " +
            "the fixture needs more sidebar content to force a multi-page float.");
        Assert.True(firstTailPage < floatEndPage,
            $"Raw Chromium printing deferred all tail content until page {firstTailPage} " +
            $"(the float doesn't finish until page {floatEndPage}) instead of reflowing it " +
            "beside the float on the earlier pages - the fixture isn't discriminating on this axis.");
    }

    [Fact]
    public async Task WithPagedJs_TailContentDoesNotReflowBesideMultiPageFloat_KnownGap()
    {
        var (pdfBytes, pagedJsPageCount) = await FixtureRunner.RunWithPagedJsDiagnosticsAsync(
            fixture.Browser, "02-float-crossing-break.html");

        Assert.True(pagedJsPageCount > 1,
            "paged.js produced no page-chunking DOM structure (.pagedjs_page) - " +
            "pagination may not have run, so this result can't be attributed to paged.js.");

        using var document = PdfDocument.Open(new MemoryStream(pdfBytes));

        var floatEndPage = FindPageContaining(document, "FLOATEND");
        var firstTailPage = FindPageContaining(document, "Tail paragraph 1.");

        // Known gap: paged.js defers ALL sibling flow content until a multi-page float
        // fully finishes, instead of reflowing it beside the float on the pages the
        // float is still occupying (which raw Chromium print does correctly - see
        // WithoutPagedJs_TailContentReflowsBesideOngoingMultiPageFloat). This wastes
        // page space on every page the float spans. If paged.js is ever fixed to
        // reflow correctly here, this assertion should start failing - flip it and
        // rename the test when that happens.
        Assert.True(firstTailPage == floatEndPage,
            "paged.js started reflowing tail content beside the float before the float finished - " +
            "the known gap this test documents may have been fixed; if so, update this test to assert " +
            "the correct (reflowing) behavior instead.");
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
