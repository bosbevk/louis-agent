using LouisAgent.Core.Logging;
using LouisAgent.Core.Logging.Content;
using LouisAgent.Packs.ShopPrices.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.ShopPrices.Prices.Store;

/// <summary>
/// Holds the price book read from the store for an hour, so a tool call reads the store at most once an hour. A store
/// that doesn't answer is logged, and the tables the pack ships stand in until the next read.
/// </summary>
/// <remarks>
/// Cache-Aside (Microsoft's cloud design patterns): the book is read from the store when it is missing or old, and kept.
/// Prices change once a month, so an hour old is fresh. One read at a time, so the store is asked once, not once per
/// waiting call.
/// </remarks>
/// <seealso href="https://learn.microsoft.com/en-us/azure/architecture/patterns/cache-aside">Cache-Aside pattern</seealso>
public sealed class PriceBookCache(IPriceStore store, TimeProvider clock)
{
    /// <summary>How long a book read from the store is kept.</summary>
    public static readonly TimeSpan Fresh = TimeSpan.FromHours(1);

    private static readonly ILogger Log = AgentLogging.For<PriceBookCache>();

    private readonly SemaphoreSlim _reading = new(1, 1);
    private (PriceBook Book, DateTimeOffset At)? _held;

    /// <summary>The price book, from the store at most an hour ago.</summary>
    public async Task<PriceBook> GetAsync(CancellationToken cancellationToken)
    {
        if (Held() is { } held) return held;
        await _reading.WaitAsync(cancellationToken);
        try
        {
            // Another call may have read it while this one waited.
            if (Held() is { } again) return again;
            var book = await ReadAsync(cancellationToken);
            _held = (book, clock.GetUtcNow());
            return book;
        }
        finally
        {
            _reading.Release();
        }
    }

    /// <summary>The book held, while it is fresh.</summary>
    private PriceBook? Held() => _held is { } held && clock.GetUtcNow() - held.At < Fresh ? held.Book : null;

    /// <summary>The store's tables as a book; the shipped tables when the store fails or holds none.</summary>
    private async Task<PriceBook> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var files = await store.LatestAsync(cancellationToken);
            return files.Count == 0 ? PriceBook.Default : new PriceBook(files);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            Log.Log(LogLevel.Warning, ShopPriceLogMessage.StoreUnavailable,
                new { Reason = LogContent.Of(ex.Message), Minutes = (int)Fresh.TotalMinutes });
            return PriceBook.Default;
        }
    }
}
