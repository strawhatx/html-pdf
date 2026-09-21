namespace core.Contracts;

public interface IOutputStorage
{
    Task SaveResultAsync(Guid jobId, byte[] pdfBytes, CancellationToken ct = default);
}