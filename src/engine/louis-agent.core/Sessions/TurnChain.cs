namespace LouisAgent.Core.Sessions;

/// <summary>
/// The agents the running turn's session was asked through, the first one first, and the session's own agent last
/// (F127.5), so an ask can refuse an agent already working on it: the cook can't ask the cook, nor ask back the voice
/// that asked it. Each agent is named as <c>AgentAddresses.ChainKeyOf</c> names it.
/// </summary>
/// <remarks>
/// Ambient context, as <see cref="TurnDepth"/> is: the host that runs a turn sets it around the turn and the tools
/// read it, so the model never carries it. Outside a turn it is empty.
/// </remarks>
public static class TurnChain
{
    private static readonly AsyncLocal<IReadOnlyList<string>?> CurrentChain = new();

    /// <summary>The running turn's chain; empty outside a turn.</summary>
    public static IReadOnlyList<string> Current => CurrentChain.Value ?? [];

    /// <summary>Runs what follows inside this chain, until the scope ends and the one before comes back.</summary>
    public static IDisposable Use(IReadOnlyList<string> chain)
    {
        var previous = CurrentChain.Value;
        CurrentChain.Value = chain;
        return new Restore(previous);
    }

    /// <summary>Puts the chain before back.</summary>
    private sealed class Restore(IReadOnlyList<string>? previous) : IDisposable
    {
        /// <summary>The scope ends.</summary>
        public void Dispose() => CurrentChain.Value = previous;
    }
}
