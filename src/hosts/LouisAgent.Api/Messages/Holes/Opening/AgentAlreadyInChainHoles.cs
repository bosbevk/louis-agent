namespace LouisAgent.Api.Messages.Holes.Opening;

/// <summary>An agent already higher up the chain of asks, and that chain.</summary>
public sealed record AgentAlreadyInChainHoles(string Agent, string Chain);
