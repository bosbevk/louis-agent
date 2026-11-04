using LouisAgent.Api.Contracts.Git.Output;

namespace LouisAgent.Api.Contracts.Git.Replies;

/// <summary>
/// The Changes tab: the checked-out branch and every changed file, and after a commit, what was committed. It is also
/// the answer to every change, so the app refreshes in one call.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record StatusReply(string? Branch, IReadOnlyList<GitFileStatus> Files, string? Committed);

/// <summary>
/// One file's diff: which file, whether it's the staged side, whether it's new, and the diff (cut off when long).
/// </summary>
public sealed record DiffReply(string Path, bool Staged, bool Untracked, string Diff, bool Truncated);
