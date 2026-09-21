using core.Contracts;

namespace renderer.Services;

/// <summary>
/// Service for storing output files, such as generated PDFs, in a local directory.
/// </summary>
public sealed class OutputStorage : IOutputStorage
{
    private readonly string _root;

    public OutputStorage()
    {
        _root = Path.Combine(AppContext.BaseDirectory, "output");
        Directory.CreateDirectory(_root);
    }

    public Task SaveResultAsync(Guid jobId, byte[] pdfBytes, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_root, $"{jobId}.pdf");
        return File.WriteAllBytesAsync(path, pdfBytes, cancellationToken);
    }
}
