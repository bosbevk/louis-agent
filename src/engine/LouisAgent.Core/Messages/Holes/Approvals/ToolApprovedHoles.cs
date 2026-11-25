namespace LouisAgent.Core.Messages.Holes.Approvals;

/// <summary>The tool approved, and where it was saved.</summary>
public sealed record ToolApprovedHoles(string Name, string Path);
