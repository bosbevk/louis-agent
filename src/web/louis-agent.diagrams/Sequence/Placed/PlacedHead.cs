using LouisAgent.Diagrams.Sequence.Spec;

namespace LouisAgent.Diagrams.Sequence.Placed;

/// <summary>A participant's head where it stands, with its label's lines; its lifeline runs down from the middle.</summary>
public sealed record PlacedHead(
    Participant Participant,
    IReadOnlyList<string> Lines,
    double X,
    double Y,
    double W,
    double H)
{
    /// <summary>Where the lifeline runs, down the head's middle.</summary>
    public double Center => X + W / 2;
}
