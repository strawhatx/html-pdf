using System.Text;
using core;

namespace api;

/// <summary>
/// Service for converting HTML or URL content to PDF.
/// </summary>
public sealed class HtmlPdfService
{
    /// <summary>
    /// Converts HTML or URL content to a PDF byte array.
    /// </summary>
    /// <param name="html">The HTML content to convert.</param>
    /// <param name="url">The URL to convert.</param>
    /// <param name="options">The conversion options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A byte array representing the generated PDF.</returns>
    public async Task<byte[]> ConvertAsync(string? html, string? url, ConvertOptions options, CancellationToken cancellationToken = default)
    {
        var hasHtml = !string.IsNullOrWhiteSpace(html);
        var hasUrl = !string.IsNullOrWhiteSpace(url);

        if (!hasHtml && !hasUrl)
        {
            throw new ArgumentException("Either Html or Url is required.");
        }

        if (hasHtml && hasUrl)
        {
            throw new ArgumentException("Provide only one of Html or Url, not both.");
        }

        if (hasUrl)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var targetUri) || !SsrfGuard.IsSchemeAllowed(targetUri))
            {
                throw new SsrfBlockedException("Url must be an absolute http(s) URL.");
            }

            if (!await SsrfGuard.IsUrlAllowedAsync(targetUri, cancellationToken))
            {
                throw new SsrfBlockedException("Url resolves to a disallowed network destination.");
            }
        }

        return await Task.FromResult(CreateMinimalPdf());
    }

    /// <summary>
    /// Creates a minimal PDF byte array. This is a placeholder implementation and 
    /// should be replaced with actual PDF generation logic.
    /// </summary>
    /// <returns>A byte array representing a minimal PDF.</returns>
    private static byte[] CreateMinimalPdf()
    {
        var header = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
        var body = Encoding.ASCII.GetBytes("%PDF-1.4\n%\u00E2\u00E3\u00CF\u00D3\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF");
        return header.Concat(body).ToArray();
    }
}
