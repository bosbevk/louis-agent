namespace LouisAgent.Orchestrator.Messages.Holes.Startup;

/// <summary>The HTTP status louis-agent's health check answered with, when it wasn't a success.</summary>
public sealed record HealthCheckFailedHoles(int Status);
