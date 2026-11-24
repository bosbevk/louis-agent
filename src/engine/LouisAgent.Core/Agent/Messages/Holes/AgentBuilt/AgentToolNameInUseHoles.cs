namespace LouisAgent.Core.Agent.Messages.Holes.AgentBuilt;

/// <summary>An agent-built tool's file whose name another tool has.</summary>
public sealed record AgentToolNameInUseHoles(string File, string Name);
