using LouisAgent.Diagrams.Sequence.Spec;

namespace LouisAgent.Diagrams.Engine.Sequence.Listed.Placed;

/// <summary>
/// The two ends of a call or an answer in a list of steps: who sends it, who it goes to, and whether the two names
/// and the line between them need a second line because they don't fit one.
/// </summary>
/// <param name="From">The participant that sends the call or the answer.</param>
/// <param name="To">The participant it goes to, the same one for a call to oneself.</param>
/// <param name="Wrapped">Whether the receiver stands on a second line, starting with the line between the two.</param>
public sealed record PlacedEnds(Participant From, Participant To, bool Wrapped)
{
    /// <summary>A call a participant makes to itself: one name, and a turn after it.</summary>
    public bool Self => From.Id == To.Id;
}
