namespace LouisAgent.Web.Chats.Replay;

/// <summary>
/// Plays stored events into a turn, each after its wait, folded the way the live chat folds them, so a page shows the
/// answer typing out as it did. The eval studio replays a stored turn with it, and a site a recorded session (F120).
/// </summary>
/// <remarks>
/// Each caller says how long each event waits (the studio from the events' own times, a recording from their kind),
/// and this loop does the rest once: apply, and ask for a redraw every third event, after any pause and at the end,
/// so a page isn't redrawn for every word.
/// </remarks>
public static class TurnReplay
{
    /// <summary>Plays the steps into the turn, asking for a redraw now and then and once at the end.</summary>
    public static async Task PlayAsync(Turn turn, IReadOnlyList<ReplayStep> steps, Action redraw,
        CancellationToken stop)
    {
        stop.ThrowIfCancellationRequested();
        int since = 0;
        foreach (var step in steps)
        {
            if (step.WaitMs > 0) await Task.Delay(step.WaitMs, stop);
            TurnStream.Apply(turn, step.Event);
            if (++since % 3 != 0 && step.WaitMs == 0) continue;

            redraw();
            since = 0;
        }

        // The last events may have fallen between redraws.
        if (since > 0) redraw();
    }
}
