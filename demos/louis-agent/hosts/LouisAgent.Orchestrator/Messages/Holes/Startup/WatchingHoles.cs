namespace LouisAgent.Orchestrator.Messages.Holes.Startup;

/// <summary>The service being watched, and the error log it writes.</summary>
public sealed record WatchingHoles(string Service, string ErrorLog);
