namespace LouisAgent.Core.Tools.Agents;

/// <summary>
/// A sub-session a chat keeps open for a follow-up (F26): where it is, what it was opened with, and how to close it.
/// <paramref name="Close"/> is the tool's own delete (the peer, the API key), so the registry that holds the entry
/// never speaks HTTP.
/// </summary>
/// <param name="Peer">The peer's base URI.</param>
/// <param name="SessionId">The session's id on the peer; never shown to the model (it uses the handle).</param>
/// <param name="Agent">The peer's name in AGENT_PEERS.</param>
/// <param name="Toolsets">The toolsets it was opened with.</param>
/// <param name="Role">Its personality: the one passed, or the one the peer derived; null when the peer didn't say.</param>
/// <param name="Close">Deletes it on the peer; never throws.</param>
public sealed record OpenSubSession(Uri Peer, string SessionId, string Agent, IReadOnlyList<string> Toolsets, string? Role, Func<Task> Close)
{
    /// <summary>The chat's name for it (<c>sub-1</c>), given by the registry when it is added.</summary>
    public string Handle { get; init; } = "";

    /// <summary>What was kept: a specialist (<see cref="SpecialistTools.Kind"/>) or a minion (<see cref="MinionTools.Kind"/>, F30).</summary>
    public string Kind { get; init; } = SpecialistTools.Kind;

    /// <summary>A minion's model, as <c>MINION_LLMS</c> named it (F30); null for a specialist or the instance's own model.</summary>
    public string? Llm { get; init; }
}
