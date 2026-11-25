namespace LouisAgent.Orchestrator.Messages.Holes.Startup;

/// <summary>The methods there are runbooks for, where louis-agent answers, and the model that triages.</summary>
public sealed record RunbooksHoles(string Methods, Uri Url, string Model);
