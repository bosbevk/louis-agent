namespace LouisAgent.Packs.Site.Messages.Holes.Forms;

/// <summary>Which field's label is wrong, what it says, how long it is, and the most it may be.</summary>
public sealed record AskBadLabelHoles(int Number, string Label, int Length, int Most);
