namespace LouisAgent.Evals.Messages.Holes.Runs;

/// <summary>The file in the runs' folder, and why it is no run.</summary>
public sealed record NotARunHoles(string File, string Reason);
