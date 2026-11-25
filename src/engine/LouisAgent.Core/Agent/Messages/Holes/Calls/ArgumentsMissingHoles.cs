namespace LouisAgent.Core.Agent.Messages.Holes.Calls;

/// <summary>The tool that wasn't run, and the arguments it still needs, one line each.</summary>
public sealed record ArgumentsMissingHoles(string Tool, string Missing);
