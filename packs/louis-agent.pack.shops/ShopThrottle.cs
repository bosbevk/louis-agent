namespace LouisAgent.Packs.Shops;

using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Packs.Shops.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// How fast one shop is searched (F34). Cook check 42d837f13659: after about 50 searches in 7 minutes Checkers answered
/// with a page that wasn't its catalogue, then nothing, for four minutes, and the cook priced 1 of 8; with searches run
/// in parallel it would come sooner. So a shop gets at most <paramref name="atOnce"/> searches at once, and once it
/// refuses one (<see cref="Refused"/>), every search waits until <paramref name="cooldown"/> has passed. One per shop
/// for the process (<see cref="ShopCatalogueFactory"/>), shared by every engine's catalogue.
/// </summary>
public sealed class ShopThrottle(int atOnce = 2, TimeSpan? cooldown = null, Func<DateTimeOffset>? now = null,
    Func<TimeSpan, CancellationToken, Task>? delay = null)
{
    private static readonly ILogger Log = AgentLogging.For<ShopThrottle>();

    private readonly SemaphoreSlim _slots = new(atOnce, atOnce);
    private readonly TimeSpan _cooldown = cooldown ?? TimeSpan.FromSeconds(20);
    private readonly Func<DateTimeOffset> _now = now ?? (() => DateTimeOffset.UtcNow);
    private readonly Func<TimeSpan, CancellationToken, Task> _delay = delay ?? Task.Delay;
    private long _notBeforeTicks;

    /// <summary>Runs <paramref name="search"/> once a slot is free and any cooldown is over.</summary>
    public async Task<T> RunAsync<T>(Func<Task<T>> search, CancellationToken cancellationToken = default)
    {
        await _slots.WaitAsync(cancellationToken);
        try
        {
            var wait = new DateTimeOffset(Interlocked.Read(ref _notBeforeTicks), TimeSpan.Zero) - _now();
            if (wait > TimeSpan.Zero) await _delay(wait, cancellationToken);
            return await search();
        }
        finally
        {
            _slots.Release();
        }
    }

    /// <summary>The shop refused a search (no answer, or a page that isn't its catalogue): the next searches wait.</summary>
    public void Refused()
    {
        Interlocked.Exchange(ref _notBeforeTicks, (_now() + _cooldown).UtcTicks);
        Log.Log(LogLevel.Warning, ShopLogMessage.ShopRefusedSearch, new { Seconds = (int)_cooldown.TotalSeconds });
    }
}
