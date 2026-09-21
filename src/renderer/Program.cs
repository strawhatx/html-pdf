using core.Contracts;
using data;
using data.Repositories;
using Microsoft.EntityFrameworkCore;
using queue;
using renderer;
using renderer.Services;

var builder = Host.CreateApplicationBuilder(args);
var dbConnection = DbConnectionOptions.FromConfiguration(builder.Configuration);
var queueOptions = builder.Configuration.GetSection("Redis").Get<QueueOptions>() ?? new QueueOptions();
queueOptions.ConnectionString = builder.Configuration.GetConnectionString("Redis")
	?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
	?? queueOptions.ConnectionString;

builder.Services.AddSingleton(queueOptions);
builder.Services.AddSingleton<RedisConnectionFactory>();
builder.Services.AddDbContext<ApiDbContext>(options => options.UseNpgsql(
	DbConnectionFactory.BuildConnectionString(
		dbConnection.Writer,
		dbConnection.Port,
		dbConnection.Database,
		dbConnection.Username,
		dbConnection.Password)));
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IUsageRepository, UsageRepository>();
builder.Services.AddSingleton<BrowserPool>();
builder.Services.AddSingleton<SsrfValidator>();
builder.Services.AddSingleton<IOutputStorage, OutputStorage>();
builder.Services.AddScoped<RenderPipeline>();
builder.Services.AddSingleton<RedisJobQueue>();
builder.Services.AddSingleton<IJobQueue>(services => services.GetRequiredService<RedisJobQueue>());
builder.Services.AddHostedService<RedisVisibilitySweepService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
