namespace LouisAgent.Api.Contracts.Git.Output;

/// <summary>
/// A local branch, and how many commits it is ahead of and behind the checked-out one; ahead 0 means it's merged.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record GitBranchInfo(
    string Name,
    string Commit,
    string Subject,
    string Author,
    string Date,
    bool Current,
    int Ahead,
    int Behind)
{
    /// <summary>Everything on it is already on the checked-out branch, so it can go.</summary>
    public bool Merged => !Current && Ahead == 0;
}
