namespace LouisAgent.Api.Endpoints.Git.Output;

/// <summary>A local branch; Ahead/Behind count commits relative to the checked-out branch (Ahead 0 = merged).</summary>
internal sealed record GitBranchInfo(string Name, string Commit, string Subject, string Author, string Date, bool Current, int Ahead, int Behind)
{
    public bool Merged => !Current && Ahead == 0;
}
