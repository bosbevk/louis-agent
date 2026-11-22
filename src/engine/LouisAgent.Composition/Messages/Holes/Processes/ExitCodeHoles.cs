namespace LouisAgent.Composition.Messages.Holes.Processes;

/// <summary>A program's exit code and what it wrote.</summary>
public sealed record ExitCodeHoles(int Code, string Output);
