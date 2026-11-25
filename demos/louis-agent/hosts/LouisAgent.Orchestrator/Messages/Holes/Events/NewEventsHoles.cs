namespace LouisAgent.Orchestrator.Messages.Holes.Events;

/// <summary>How many new errors came in, and the run id their usage is recorded under.</summary>
public sealed record NewEventsHoles(int Count, string RunId);
