namespace LouisAgent.Orchestrator.Messages.Holes.Events;

/// <summary>What kept the service's repository from going back to its main branch.</summary>
public sealed record NotBackOnMainHoles(string Problem);
