namespace LouisAgent.Orchestrator.Messages.Holes.Events;

/// <summary>The error whose triage failed, and why.</summary>
public sealed record TriageFailedHoles(string ErrorId, string Reason);
