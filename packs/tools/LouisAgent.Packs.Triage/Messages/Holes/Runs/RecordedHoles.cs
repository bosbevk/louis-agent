namespace LouisAgent.Packs.Triage.Messages.Holes.Runs;

/// <summary>A step saved to a run: its number, the run, and what it says.</summary>
public sealed record RecordedHoles(int Number, string RunId, string Summary);
