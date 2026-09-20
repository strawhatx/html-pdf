namespace api;

/// <summary>
/// Provides API key authentication for the HTML to PDF service.
/// </summary>
public sealed class ApiKeyAuthentication
{
    public string? CurrentKey { get; private set; }

    public void SetKey(string? key)
    {
        CurrentKey = key;
    }
}

/// <summary>
/// Middleware for API key authentication.
/// </summary>
public sealed class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiKeyAuthentication _auth;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyAuthenticationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="auth">The API key authentication instance.</param>
    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ApiKeyAuthentication auth)
    {
        _next = next;
        _auth = auth;
    }

    /// <summary>
    /// Invokes the middleware to authenticate the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that represents the completion of request processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var apiKey = context.Request.Headers["x-api-key"].FirstOrDefault()
            ?? context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);

        var configuredKey = Environment.GetEnvironmentVariable("HTMLPDF_API_KEY");
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            _auth.SetKey(apiKey);
            await _next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(apiKey) || !string.Equals(apiKey, configuredKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API key." });
            return;
        }

        _auth.SetKey(apiKey);
        await _next(context);
    }
}
