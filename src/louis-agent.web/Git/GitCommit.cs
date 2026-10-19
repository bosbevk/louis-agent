namespace LouisAgent.Web.Git;

public sealed record GitCommit(string Sha, string ShortSha, string Author, string Date, string Subject, List<string> Refs, List<string> Parents)
{
    public bool IsMerge => Parents.Count > 1;
}
