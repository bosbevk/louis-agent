using System.Collections.Concurrent;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Replay.Store;

namespace LouisAgent.Core.Tools.Web.Replay;

/// <summary>
/// The clock, recorded or replayed (F18 step 4b), by <see cref="ReplayTrial.Current"/> on each read. With no trial it answers the inner clock
/// (<see cref="TimeProvider.System"/> by default), saving and freezing nothing. With a trial the clock stands still within it: the first
/// read is the one answer, Record reading the inner clock and saving it, Replay reading the saved value, and every later read in that trial
/// answers the same time. A replay would otherwise show the model a different time than the recording did, and step 4e's cache of model
/// answers would miss. A recording that never read the clock leaves a replay with a miss, which throws <see cref="WebToolMessage.ReplayClockMiss"/>;
/// a <see cref="Lazy{T}"/> keeps its exception, so a miss stays a miss for the rest of the trial (intended).
/// </summary>
/// <remarks>
/// <see cref="TimeProvider"/> is synchronous and the store is async, so the one store call per trial blocks with
/// <c>GetAwaiter().GetResult()</c>. That is safe here: ASP.NET Core has no synchronization context to deadlock on, and it happens once per
/// trial. The <see cref="Lazy{T}"/> is what lets a parent and its sub-session, reading at the same moment, save one line and not two. The
/// dictionary keeps one small entry per trial until the process restarts.
/// </remarks>
public sealed class ReplayClock(WebReplayStore store, TimeProvider? inner = null) : TimeProvider
{
    private static readonly ReplayAsk Ask = ReplayAsk.Of(ReplayKind.Clock, new { });

    private readonly TimeProvider _inner = inner ?? System;
    private readonly ConcurrentDictionary<(string Of, WebReplayMode Mode), Lazy<DateTimeOffset>> _times = new();

    public override DateTimeOffset GetUtcNow() =>
        ReplayTrial.Current is not { } trial
            ? _inner.GetUtcNow()
            : _times.GetOrAdd((trial.Of, trial.Mode), _ => new Lazy<DateTimeOffset>(() => Read(trial))).Value;

    private DateTimeOffset Read(ReplayTrial trial)
    {
        if (!trial.Replaying)
        {
            var now = _inner.GetUtcNow();
            store.SaveAnswerAsync(trial.Of, Ask, now).GetAwaiter().GetResult();
            return now;
        }

        if (store.FindAsync<DateTimeOffset>(trial.Of, Ask).GetAwaiter().GetResult() is not { } recorded)
            throw new InvalidOperationException(MessageText.Format(WebToolMessage.ReplayClockMiss));
        return recorded.OrThrow(error => new InvalidOperationException(error));
    }
}
