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

    /// <summary>
    /// Initializes the browser pool by launching a Chromium instance. This method should be called before acquiring any browser contexts. It ensures that the browser is ready for use and handles any necessary setup.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await GetOrCreateBrowserAsync(cancellationToken);
    }

    /// <summary>
    ///  Acquires a new browser context from the pool. This method waits for an available slot in the concurrency limiter, creates a new browser context, and returns it wrapped in a <see cref="RentedContext"/>. The caller is responsible for disposing the rented context to release the slot back to the pool.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

/// <summary>
/// Gets or creates the Chromium browser instance. If the browser is already running and connected, it returns the existing instance. If not, it launches a new Chromium process with the specified options. This method ensures that only one browser instance is created and handles concurrency using a semaphore lock.
/// </summary>
/// <param name="cancellationToken"></param>
/// <returns></returns>
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

            // Launch a new Chromium instance with the specified options
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

    /// <summary>
    /// Disposes the browser pool and releases all resources. This method closes the Chromium browser instance and disposes of the Playwright instance. It should be called when the browser pool is no longer needed to free up system resources.
    /// </summary>
    /// <returns></returns>
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
