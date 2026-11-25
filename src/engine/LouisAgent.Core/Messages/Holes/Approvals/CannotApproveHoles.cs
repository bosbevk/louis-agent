namespace LouisAgent.Core.Messages.Holes.Approvals;

/// <summary>A tool that can't be approved, and what is wrong with its definition.</summary>
public sealed record CannotApproveHoles(string Name, string Errors);
