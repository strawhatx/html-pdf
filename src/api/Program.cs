using api;
using core;
using core.Jobs;
using data;
using data.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var dbConnection = DbConnectionOptions.FromConfiguration(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<ApiDbContext>(options => options.UseNpgsql(
    DbConnectionFactory.BuildConnectionString(
        dbConnection.Writer,
        dbConnection.Port,
        dbConnection.Database,
        dbConnection.Username,
        dbConnection.Password)));
builder.Services.AddSingleton<HtmlPdfService>();
builder.Services.AddSingleton<ApiKeyAuthentication>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IUsageRepository, UsageRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

/// <summary>
/// Endpoint for converting HTML to PDF.
/// </summary>
app.MapPost("/v1/convert", async (ConvertRequest request, HtmlPdfService service, IJobRepository jobs, ApiKeyAuthentication auth, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    try
    {
        var bytes = await service.ConvertAsync(request.Html, request.Url, request.Options ?? new ConvertOptions());
        var job = new RenderJob
        {
            ApiKeyId = auth.CurrentKey ?? "anonymous",
            Html = request.Html,
            Url = request.Url,
            Options = new RenderOptions
            {
                PageSize = request.Options?.PageSize ?? "A4",
                HeaderHtml = request.Options?.Header,
                FooterHtml = request.Options?.Footer,
            },
        };
        await jobs.CreateAsync(job, cancellationToken);
        await jobs.UpdateStatusAsync(job.Id, JobStatus.Completed, ct: cancellationToken);
        return Results.File(bytes, "application/pdf", "document.pdf");
    }
    catch (SsrfBlockedException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled error converting HTML to PDF");
        return Results.Problem(detail: "PDF conversion failed.", statusCode: StatusCodes.Status500InternalServerError);
    }
});

/// <summary>
/// Endpoint for retrieving job information.
/// </summary>
app.MapGet("/v1/jobs/{id:guid}", async (Guid id, IJobRepository jobs, CancellationToken cancellationToken) =>
{
    var job = await jobs.GetAsync(id, cancellationToken);
    return job is null
        ? Results.NotFound(new { error = "Job not found." })
        : Results.Ok(job);
});

/// <summary>
/// Endpoint for retrieving usage information for the current API key.
/// </summary>
app.MapGet("/v1/usage", async (ApiKeyAuthentication auth, IUsageRepository usage, CancellationToken cancellationToken) =>
{
    var key = auth.CurrentKey ?? "anonymous";
    var seconds = await usage.TotalConcurrentSecondsAsync(key, cancellationToken);
    return Results.Ok(new { key, concurrentSeconds = seconds });
});

app.Run();

public partial class Program { }
