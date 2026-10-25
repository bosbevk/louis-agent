namespace LouisAgent.Api.Endpoints.Git.Output;

internal sealed record GitCommitInfo(string Sha, string ShortSha, string Author, string Date, string Subject, List<string> Refs, List<string> Parents);
