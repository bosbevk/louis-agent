using LouisAgent.Api.Contracts.Git.Output;

namespace LouisAgent.Api.Contracts.Git.Replies;

/// <summary>The log across all branches, newest first.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record CommitsReply(IReadOnlyList<GitCommitInfo> Commits);

/// <summary>One commit: the commit, its whole message, and its diff (cut off when long).</summary>
public sealed record CommitReply(GitCommitInfo Commit, string Body, string Diff, bool Truncated);
