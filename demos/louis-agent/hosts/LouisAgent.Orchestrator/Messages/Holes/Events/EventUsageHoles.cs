namespace LouisAgent.Orchestrator.Messages.Holes.Events;

/// <summary>What handling one error used, triage and fix together, written for reading.</summary>
public sealed record EventUsageHoles(string ErrorId, string Usage);
