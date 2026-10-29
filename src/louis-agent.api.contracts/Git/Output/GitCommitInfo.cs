namespace LouisAgent.Api.Contracts.Git.Output;

/// <summary>
/// One commit as the log shows it: its sha, who and when, its subject, the names pointing at it, its parents.
/// </summary>
public sealed record GitCommitInfo(
    string Sha,
    string ShortSha,
    string Author,
    string Date,
    string Subject,
    List<string> Refs,
    List<string> Parents);
