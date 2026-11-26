namespace LouisAgent.Packs.Triage.Messages.Holes.Runs;

/// <summary>One step of a run as a line: its number, who, what kind, when, what and why.</summary>
public sealed record StepLineHoles(int Number, string Agent, string Kind, string At, string Summary, string Why);
