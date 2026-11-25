namespace LouisAgent.Orchestrator.Messages.Holes.Events;

/// <summary>What triage decided for an error, by its wire name, and why.</summary>
public sealed record DecisionHoles(string ErrorId, string Outcome, string Reason);
