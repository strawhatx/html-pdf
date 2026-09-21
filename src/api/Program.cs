using api;
using core;
using data;

var builder = WebApplication.CreateBuilder(args);
var dbConnection = DbConnectionOptions.FromConfiguration(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IDbConnectionFactory>(_ =>
    new DbConnectionFactory(
        dbConnection.Reader,
        dbConnection.Writer,
        dbConnection.Port,
        dbConnection.Database,
        dbConnection.Username,
        dbConnection.Password));
builder.Services.AddSingleton<HtmlPdfService>();
builder.Services.AddSingleton<ApiKeyAuthentication>();
builder.Services.AddSingleton<JobStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/v1/convert", async (ConvertRequest request, HtmlPdfService service, JobStore jobStore, ILogger<Program> logger) =>
{
    try
    {
        var bytes = await service.ConvertAsync(request.Html, request.Url, request.Options ?? new ConvertOptions());
        var jobId = jobStore.CreateJob("completed", bytes.Length);
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

app.MapGet("/v1/jobs/{id}", (string id, JobStore jobStore) =>
{
    var job = jobStore.Get(id);
    return job is null
        ? Results.NotFound(new { error = "Job not found." })
        : Results.Ok(job);
});

app.MapGet("/v1/usage", (ApiKeyAuthentication auth, JobStore jobStore) =>
{
    var key = auth.CurrentKey ?? "anonymous";
    var usage = jobStore.UsageFor(key);
    return Results.Ok(usage);
});

app.Run();

public partial class Program { }
