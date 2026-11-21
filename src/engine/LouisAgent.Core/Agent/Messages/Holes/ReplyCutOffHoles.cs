namespace LouisAgent.Core.Agent.Messages.Holes;

/// <summary>The output limit a reply ran into, for the log line that says the model is asked to go on.</summary>
public sealed record ReplyCutOffHoles(int MaxOutputTokens);
