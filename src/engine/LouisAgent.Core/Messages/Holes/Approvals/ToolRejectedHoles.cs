namespace LouisAgent.Core.Messages.Holes.Approvals;

/// <summary>The tool rejected, and what more was done about it, if anything.</summary>
public sealed record ToolRejectedHoles(string Name, string Removed);
