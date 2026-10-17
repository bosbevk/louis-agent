namespace LouisAgent.Core.Sessions;

/// <summary>
/// Where a turn's sub-session events go while it runs: a host that shows them (the API's chat stream) observes the turn,
/// and <see cref="SpecialistTools"/> reports every event it reads. Held in an <see cref="AsyncLocal{T}"/>, like the usage
/// scope, so it flows into the tool call without being passed through the engine; nothing observes it by default.
/// </summary>
public static class SubSessionFeed
{
    private static readonly AsyncLocal<Action<SubSessionEvent>?> CurrentObserver = new();

    /// <summary>Sends this flow's sub-session events to <paramref name="observer"/> until the result is disposed.</summary>
    public static IDisposable Observe(Action<SubSessionEvent> observer)
    {
        var previous = CurrentObserver.Value;
        CurrentObserver.Value = observer;
        return new Restore(previous);
    }

    /// <summary>Reports an event to the current observer, if any; a failing observer never breaks the tool.</summary>
    internal static void Report(SubSessionEvent subSessionEvent)
    {
        try { CurrentObserver.Value?.Invoke(subSessionEvent); }
        catch (Exception) { }
    }

    private sealed class Restore(Action<SubSessionEvent>? previous) : IDisposable
    {
        public void Dispose() => CurrentObserver.Value = previous;
    }
}
