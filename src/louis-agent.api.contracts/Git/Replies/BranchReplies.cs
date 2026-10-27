using LouisAgent.Api.Contracts.Git.Output;

namespace LouisAgent.Api.Contracts.Git.Replies;

/// <summary>
/// The Branches tab: the checked-out branch, every branch, and after a merge or a delete, what was done.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record BranchesReply(string? Current, IReadOnlyList<GitBranchInfo> Branches, string? Message);

/// <summary>
/// One branch: what it would bring in if merged into the checked-out one, as its commits and its diff (cut off when
/// too long).
/// </summary>
public sealed record BranchDiffReply(
    GitBranchInfo Branch,
    string? Into,
    IReadOnlyList<GitCommitInfo> Commits,
    string Diff,
    bool Truncated);
