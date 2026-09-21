namespace renderer.Services;

public sealed class SsrfViolationException(string message) : InvalidOperationException(message);
