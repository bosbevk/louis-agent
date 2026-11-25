namespace LouisAgent.Core.Agent.Messages.Holes.Commands;

/// <summary>How long, in milliseconds, a skill's command may run before it is stopped.</summary>
public sealed record CommandTimeoutHoles(int Milliseconds);
