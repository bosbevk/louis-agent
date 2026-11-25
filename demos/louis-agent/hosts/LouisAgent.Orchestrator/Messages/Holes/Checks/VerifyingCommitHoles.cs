namespace LouisAgent.Orchestrator.Messages.Holes.Checks;

/// <summary>The commit being checked, and the branch it is on.</summary>
public sealed record VerifyingCommitHoles(string Commit, string Branch);
