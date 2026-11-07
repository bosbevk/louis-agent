namespace LouisAgent.Packs.Git.Web.Branches.Records;

/// <summary>One commit as the log lists it, with the branches and tags that point at it.</summary>
public sealed record GitCommit(string Sha, string ShortSha, string Author, string Date, string Subject, List<string> Refs, List<string> Parents)
{
    /// <summary>A commit with more than one parent is a merge.</summary>
    public bool IsMerge => Parents.Count > 1;
}
