using LouisAgent.Api.Messages;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Evals.Store.Recording;

/// <summary>
/// One run's writes to the store. After the first failure it stops trying, so a store that's down never slows the run,
/// and it remembers why, for the run and the log.
/// </summary>
/// <remarks>
/// A circuit breaker that opens once and stays open for the run: the run carries on without its store, and its
/// result says what wasn't kept, instead of every later write failing and logging again.
/// </remarks>
internal sealed class GuardedWrites(string run, ILogger? logger)
{
    /// <summary>Why the store stopped taking writes, or nothing while it still does.</summary>
    public string? StoreError { get; private set; }

    /// <summary>Tries one write, unless an earlier one already failed.</summary>
    public async Task WriteAsync(Func<Task> write)
    {
        if (StoreError is not null) return;
        try
        {
            await write();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Failed(ex);
        }
    }

    /// <summary>Remembers the first failure and logs it; later ones add nothing new.</summary>
    public void Failed(Exception ex)
    {
        if (StoreError is not null) return;
        StoreError = MessageText.Format(ApiUserText.BenchmarkStoreFailed, new { Reason = ex.Message });
        logger?.Log(LogLevel.Warning, ApiLogMessage.BenchmarkStoreFailed, new { Run = run }, exception: ex, tag: "API");
    }
}
