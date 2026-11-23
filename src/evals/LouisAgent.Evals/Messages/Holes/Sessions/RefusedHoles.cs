namespace LouisAgent.Evals.Messages.Holes.Sessions;

/// <summary>The status the API answered with, and its own reason.</summary>
public sealed record RefusedHoles(int Status, string Reason);
