using UglyToad.PdfPig;

namespace fixtureSuite.Tests;

[Collection("Playwright")]
public sealed class Fixture03OrphansWidowsTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task WithOrphansWidowsSet_NeverStrandsFewerThan3Lines()
    {
        var pdfBytes = await FixtureRunner.RunAsync(fixture.Browser, "03-orphans-widows.html");
        using var document = PdfDocument.Open(new MemoryStream(pdfBytes));

        var lines = ExtractLinesBetweenHeadings(
            document,
            startsAfter: "WITH orphans/widows",
            endsBefore: "WITHOUT orphans/widows");
        var pages = lines.Select(line => line.Page).Distinct().OrderBy(page => page).ToList();

        Assert.True(pages.Count >= 2, "Fixture 03 did not produce a page transition to test.");

        foreach (var pagePair in pages.Zip(pages.Skip(1)))
        {
            var linesBefore = lines.Count(line => line.Page == pagePair.First);
            var linesAfter = lines.Count(line => line.Page == pagePair.Second);

            Assert.True(linesBefore >= 3,
                $"Widow violation: only {linesBefore} line(s) landed on page {pagePair.First} before the break.");
            Assert.True(linesAfter >= 3,
                $"Orphan violation: only {linesAfter} line(s) landed on page {pagePair.Second} after the break.");
        }
    }

    private sealed record LineInfo(int Page, double Y, string Text);

    private static List<LineInfo> ExtractLinesBetweenHeadings(
        PdfDocument document,
        string startsAfter,
        string endsBefore)
    {
        var lines = new List<LineInfo>();
        var inSection = false;

        foreach (var page in document.GetPages())
        {
            var wordsByLine = page.GetWords()
                .GroupBy(word => Math.Round(word.BoundingBox.Bottom, 0))
                .OrderByDescending(group => group.Key);

            foreach (var lineGroup in wordsByLine)
            {
                var lineText = string.Join(" ", lineGroup.Select(word => word.Text));

                if (!inSection && lineText.Contains(startsAfter, StringComparison.Ordinal))
                {
                    inSection = true;
                    continue;
                }

                if (inSection && lineText.Contains(endsBefore, StringComparison.Ordinal))
                {
                    return lines;
                }

                if (inSection)
                {
                    lines.Add(new LineInfo(page.Number, lineGroup.Key, lineText));
                }
            }
        }

        return lines;
    }
}