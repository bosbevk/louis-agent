namespace LouisAgent.Orchestrator.Messages.Holes.Checks;

/// <summary>The service method called again, and the order it was called for.</summary>
public sealed record ReplayingHoles(string Method, string OrderId);
