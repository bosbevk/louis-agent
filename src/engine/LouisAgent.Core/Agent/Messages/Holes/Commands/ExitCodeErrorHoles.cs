namespace LouisAgent.Core.Agent.Messages.Holes.Commands;

/// <summary>A command that failed: its exit code, and what it wrote.</summary>
public sealed record ExitCodeErrorHoles(int Code, string Output);
