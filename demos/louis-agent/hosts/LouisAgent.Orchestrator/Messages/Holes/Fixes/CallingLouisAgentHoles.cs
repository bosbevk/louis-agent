namespace LouisAgent.Orchestrator.Messages.Holes.Fixes;

/// <summary>Where louis-agent is asked to fix an error, what failed where, and the branch the fix goes on.</summary>
public sealed record CallingLouisAgentHoles(Uri Url, string ExceptionType, string Method, string Branch);
