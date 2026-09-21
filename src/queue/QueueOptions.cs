namespace queue;

public sealed class QueueOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public string PendingListKey { get; set; } = "pdfapi:jobs:pending";
    public string ProcessingSetKey { get; set; } = "pdfapi:jobs:processing";
    public int VisibilityTimeoutSeconds { get; set; } = 300;
    public string PayloadHashKey { get; set; } = "pdfapi:jobs:payloads";
}