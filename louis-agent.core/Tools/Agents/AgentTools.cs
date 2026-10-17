namespace LouisAgent.Core.Tools.Agents;

using LouisAgent.Core.Sessions;

/// <summary>
/// The running agent's own tool names, set per turn (F30): the host (the API's session) sets them for a turn, and
/// <see cref="MinionTools.StartMinion"/> refuses, before any HTTP, a tool the specialist doesn't hold itself: a minion can
/// never do more than the specialist that sent it. Held in an <see cref="AsyncLocal{T}"/>, like
/// <see cref="SessionAttachments"/>; empty by default, so outside a host's turn no tool can be handed down.
/// </summary>
public static class AgentTools
{
    private static readonly IReadOnlySet<string> None = new HashSet<string>(StringComparer.Ordinal);
    private static readonly AsyncLocal<IReadOnlySet<string>?> CurrentTools = new();

    /// <summary>Makes <paramref name="tools"/> this flow's tool names until the result is disposed.</summary>
    public static IDisposable Use(IEnumerable<string> tools)
    {
        var previous = CurrentTools.Value;
        CurrentTools.Value = tools.ToHashSet(StringComparer.Ordinal);
        return new Restore(previous);
    }

    /// <summary>The tool names of the session in this flow; none when no host set them.</summary>
    internal static IReadOnlySet<string> Current => CurrentTools.Value ?? None;

    private sealed class Restore(IReadOnlySet<string>? previous) : IDisposable
    {
        public void Dispose() => CurrentTools.Value = previous;
    }
}
