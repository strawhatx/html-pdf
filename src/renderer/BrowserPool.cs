using Microsoft.Playwright;
using System.Diagnostics;

namespace renderer;

/// <summary>
/// Keeps one warm Chromium process alive and hands out a fresh, isolated
/// <see cref="IBrowserContext"/> per job so one caller's HTML can't leak state into another's,
/// without paying Chromium's multi-second process cold-start cost on every request. Concurrent
/// context count is capped so request bursts queue on a semaphore instead of exhausting host
/// memory (each Chromium context runs alongside a shared ~85-200MB process).
/// </summary>
public sealed class BrowserPool : IAsyncDisposable
{
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public BrowserPool(int? configuredMaxConcurrentContexts = null)
    {
        var configured = configuredMaxConcurrentContexts;
        var max = configured is > 0 ? configured.Value : Math.Max(2, Environment.ProcessorCount);
        _concurrencyLimiter = new SemaphoreSlim(max, max);
    }

    public async Task<RentedContext> AcquireContextAsync(CancellationToken cancellationToken = default)
    {
        await _concurrencyLimiter.WaitAsync(cancellationToken);

        try
        {
            var browser = await GetOrCreateBrowserAsync(cancellationToken);
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                JavaScriptEnabled = true,
                Permissions = [],
            });

            return new RentedContext(context, _concurrencyLimiter);
        }
        catch
        {
            _concurrencyLimiter.Release();
            throw;
        }
    }

    private async Task<IBrowser> GetOrCreateBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser is { IsConnected: true })
        {
            return _browser;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_browser is { IsConnected: true })
            {
                return _browser;
            }

            if (_browser is not null)
            {
                Console.Error.WriteLine("Chromium instance disconnected; relaunching browser pool.");
                await _browser.DisposeAsync();
            }

            _playwright ??= await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args =
                [
                    "--no-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-extensions",
                ],
            });

            return _browser;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }
}

/// <summary>
/// A leased browser context. Disposing it closes the context (dropping any state the job's HTML
/// created — cookies, storage, in-flight requests) and releases its concurrency slot.
/// </summary>
public sealed class RentedContext : IAsyncDisposable
{
    private readonly SemaphoreSlim _limiter;
    private int _disposed;

    public IBrowserContext Context { get; }

    internal RentedContext(IBrowserContext context, SemaphoreSlim limiter)
    {
        Context = context;
        _limiter = limiter;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        try
        {
            await Context.CloseAsync();
        }
        finally
        {
            _limiter.Release();
        }
    }
}
