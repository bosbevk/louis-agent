namespace LouisAgent.Web.Chats;

/// <summary>
/// Keeps a stream's redraws to one per window, and always draws the last update. Without that last part an update just
/// after a redraw waits for the next event, which can be half a minute away while a tool runs, so the page sits behind
/// and then seems to print everything at once.
/// </summary>
/// <remarks>
/// A throttle with a trailing call: the first update in a window draws at once, later ones share one catch-up when the
/// window ends. A plain throttle drops them, and redrawing on every event rebuilds the whole chat per token. It takes
/// its clock from outside, so a test can move time and see the catch-up fire.
/// </remarks>
public sealed class RedrawThrottle(TimeProvider clock, TimeSpan every, Action redraw)
{
    private readonly object _gate = new();
    private DateTimeOffset _last;
    private ITimer? _catchUp;

    /// <summary>Something changed, so the page should show it.</summary>
    public void Request()
    {
        lock (_gate)
        {
            TimeSpan since = clock.GetUtcNow() - _last;
            if (since < every)
            {
                _catchUp ??= clock.CreateTimer(_ => CatchUp(), null, every - since, Timeout.InfiniteTimeSpan);
                return;
            }

            Drew();
        }

        redraw();
    }

    /// <summary>The turn is over, and the last redraw is the runner's own.</summary>
    public void Stop()
    {
        lock (_gate) CancelCatchUp();
    }

    /// <summary>The window ended with an update still waiting, so it's drawn now.</summary>
    private void CatchUp()
    {
        lock (_gate)
        {
            if (_catchUp is null) return;
            Drew();
        }

        redraw();
    }

    /// <summary>A redraw is about to happen, which covers whatever was waiting and starts a new window.</summary>
    private void Drew()
    {
        CancelCatchUp();
        _last = clock.GetUtcNow();
    }

    /// <summary>Nothing is waiting any more.</summary>
    private void CancelCatchUp()
    {
        _catchUp?.Dispose();
        _catchUp = null;
    }
}
