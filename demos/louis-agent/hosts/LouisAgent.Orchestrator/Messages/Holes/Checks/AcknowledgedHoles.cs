namespace LouisAgent.Orchestrator.Messages.Holes.Checks;

/// <summary>The error triage let go, and why it needs nothing done.</summary>
public sealed record AcknowledgedHoles(string ErrorId, string Reason);
