namespace core.Errors;

public enum ErrorCode
{
    InvalidRequest,
    MissingInput,
    UnsupportedWaitCondition,
    SsrfBlocked,
    RenderFailed,
    RenderTimeout,
    OutputStorageFailed,
}