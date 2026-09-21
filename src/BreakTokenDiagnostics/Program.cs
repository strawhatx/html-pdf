using Microsoft.Playwright;

if (args.Length < 1)
{
	Console.WriteLine("Usage: dotnet run -- <input.html> [--out output.pdf]");
	return 1;
}

var inputPath = args[0];
var outIndex = Array.IndexOf(args, "--out");
var outputPath = outIndex >= 0 && outIndex + 1 < args.Length
	? args[outIndex + 1]
	: Path.ChangeExtension(Path.GetFileName(inputPath), ".pdf");

if (!File.Exists(inputPath))
{
	Console.Error.WriteLine($"File not found: {inputPath}");
	return 1;
}

var html = await File.ReadAllTextAsync(inputPath);
var bundlePath = Path.Combine(AppContext.BaseDirectory, "Assets", "pagedjs", "paged.v0.1.0.js");

if (!File.Exists(bundlePath))
{
	Console.Error.WriteLine($"PagedJS asset not found: {bundlePath}");
	return 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
var page = await browser.NewPageAsync();

var stopwatch = System.Diagnostics.Stopwatch.StartNew();

await page.SetContentAsync(html);
await page.AddScriptTagAsync(new() { Path = bundlePath });

try
{
	await page.EvaluateAsync("() => new Paged.Previewer().preview()");
}
catch (TimeoutException)
{
	Console.Error.WriteLine("Pagination did not complete within 30s - check the fixture for infinite-loop conditions.");
	return 1;
}

var pdfBytes = await page.PdfAsync(new() { PrintBackground = true, Format = "A4" });
await File.WriteAllBytesAsync(outputPath, pdfBytes);

stopwatch.Stop();
Console.WriteLine($"OK - wrote {outputPath} ({pdfBytes.Length:N0} bytes) in {stopwatch.ElapsedMilliseconds}ms");
return 0;
