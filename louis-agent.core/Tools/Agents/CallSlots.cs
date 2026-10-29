
using System.Collections.Concurrent;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Agents.Messages;
using LouisAgent.Core.Usage.Records;

namespace LouisAgent.Core.Tools.Agents;
/// <summary>
/// The sub-session calls a turn may make (S, CL15, from SpecialistTools): at most <paramref name="maxPerTurn"/> per turn
/// (starts and continues alike); outside a turn (an MCP call), the most <paramref name="noTurnWindow"/> may.
/// </summary>
internal sealed class CallSlots(int maxPerTurn, TimeSpan noTurnWindow, TimeProvider time)
{
    private readonly ConcurrentDictionary<string, int> _perTurn = new(StringComparer.Ordinal);
    private readonly Queue<DateTimeOffset> _noTurn = new();

    /// <summary>Takes one slot; returns the refusal when there is none, and the turn's parent tag either way.</summary>
    public string? Take(UsageScope? scope, out string? parent)
    {
        parent = ParentTag(scope);
        if (parent is null ? !TakeNoTurnSlot() : !TakeTurnSlot(parent))
            return MessageText.Format(
                parent is null ? SubSessionToolMessage.SubSessionRateLimit : SubSessionToolMessage.SubSessionLimit,
                new { Max = maxPerTurn });
        return null;
    }

    /// <summary>This turn as a child's <c>parent</c> tag, <c>{session}#{turn}</c>; null outside a session's turn.</summary>
    public static string? ParentTag(UsageScope? scope) =>
        scope?.Session is { } session ? $"{session}#{scope.Turn}" : null;

    /// <summary>Outside a turn there's nothing to count against, so a rolling window: at most maxPerTurn a window.</summary>
    private bool TakeNoTurnSlot()
    {
        lock (_noTurn)
        {
            DateTimeOffset now = time.GetUtcNow();
            while (_noTurn.Count > 0 && now - _noTurn.Peek() >= noTurnWindow) _noTurn.Dequeue();
            if (_noTurn.Count >= maxPerTurn) return false;
            _noTurn.Enqueue(now);
            return true;
        }
    }

    private bool TakeTurnSlot(string turn)
    {
        // Old turns' counts are dropped now and then; a turn never needs more than its few entries.
        if (_perTurn.Count > 1000) _perTurn.Clear();
        return _perTurn.AddOrUpdate(turn, 1, (_, n) => n + 1) <= maxPerTurn;
    }
}
