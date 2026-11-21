namespace LouisAgent.Core.Sessions;

/// <summary>
/// How deep the running turn's session sits (F127.5): 0 for the agent a person talks to, one more for each agent
/// asked below it, so an ask opens its child one level down from wherever it is asked.
/// </summary>
/// <remarks>
/// Ambient context, as <see cref="UserPlace"/> is: the host that runs a turn sets it around the turn and the tools
/// read it, so the model never carries it. Outside a turn it is 0, as a person's own session is.
/// </remarks>
public static class TurnDepth
{
    private static readonly AsyncLocal<int> CurrentDepth = new();

    /// <summary>The running turn's depth; 0 outside a turn.</summary>
    public static int Current => CurrentDepth.Value;

    /// <summary>Runs what follows at this depth, until the scope ends and the one before comes back.</summary>
    public static IDisposable Use(int depth)
    {
        int previous = CurrentDepth.Value;
        CurrentDepth.Value = depth;
        return new Restore(previous);
    }

    /// <summary>Puts the depth before back.</summary>
    private sealed class Restore(int previous) : IDisposable
    {
        /// <summary>The scope ends.</summary>
        public void Dispose() => CurrentDepth.Value = previous;
    }
}
