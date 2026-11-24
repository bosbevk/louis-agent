namespace LouisAgent.Core.Agent.Messages.Holes.Summaries;

/// <summary>A long answer that couldn't be summarised, and why.</summary>
public sealed record SummaryFailedHoles(string Tool, string Reason);
