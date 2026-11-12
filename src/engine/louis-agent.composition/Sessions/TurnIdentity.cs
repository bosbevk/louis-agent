using LouisAgent.Composition.Packs;

namespace LouisAgent.Composition.Sessions;

/// <summary>
/// Who a turn runs as, as the tools in it can read it (F127.2, Louis's pick "the identity says so"): the session's
/// identity, by name, and whether that identity is a demonstration, so a tool that acts for a business (the leads
/// pack's) can tell a demonstration's request from a real one without being told by the model.
/// </summary>
/// <remarks>
/// Ambient context, held in an <see cref="AsyncLocal{T}"/> as the visitor's place (<c>UserPlace</c>) is: the host
/// that runs a turn sets it around the turn, and a tool reads it, so neither the tool's arguments nor the model carry
/// it, and a pack still names no identity. None outside a turn.
/// </remarks>
/// <param name="Name">The identity's name, as the catalogue knows it.</param>
/// <param name="IsDemonstration">
/// Whether the identity says it's a demonstration (<see cref="IAgentIdentity.IsDemonstration"/>).
/// </param>
/// <param name="EndsEachAnswerWith">
/// The tool each answer ends with (<see cref="IAgentIdentity.EndsEachAnswerWith"/>, FX139), if the identity names
/// one.
/// </param>
public sealed record TurnIdentity(string Name, bool IsDemonstration, string? EndsEachAnswerWith = null)
{
    private static readonly AsyncLocal<TurnIdentity?> CurrentIdentity = new();

    /// <summary>Who the running turn runs as; none outside a turn.</summary>
    public static TurnIdentity? Current => CurrentIdentity.Value;

    /// <summary>Who a session in this identity runs as.</summary>
    public static TurnIdentity Of(IAgentIdentity identity) =>
        new(identity.Name, identity.IsDemonstration, identity.EndsEachAnswerWith);

    /// <summary>Runs what follows as this identity, until the scope ends and the one before comes back.</summary>
    public static IDisposable Use(TurnIdentity? identity)
    {
        var previous = CurrentIdentity.Value;
        CurrentIdentity.Value = identity;
        return new Restore(previous);
    }

    /// <summary>Puts the identity before back.</summary>
    private sealed class Restore(TurnIdentity? previous) : IDisposable
    {
        /// <summary>The scope ends.</summary>
        public void Dispose() => CurrentIdentity.Value = previous;
    }
}
