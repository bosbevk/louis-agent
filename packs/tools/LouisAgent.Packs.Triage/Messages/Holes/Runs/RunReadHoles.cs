namespace LouisAgent.Packs.Triage.Messages.Holes.Runs;

/// <summary>A run read back: its id, how many steps, how it ended if it has, and its steps one line each.</summary>
public sealed record RunReadHoles(string RunId, int Count, string Ended, string Steps);
