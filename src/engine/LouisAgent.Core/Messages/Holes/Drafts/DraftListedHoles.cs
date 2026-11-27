namespace LouisAgent.Core.Messages.Holes.Drafts;

/// <summary>A draft waiting: its name, what it does, and the call it was proved with and its answer.</summary>
public sealed record DraftListedHoles(string Name, string Description, string Example);
