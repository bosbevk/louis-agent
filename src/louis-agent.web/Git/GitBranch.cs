namespace LouisAgent.Web.Git;

/// <summary>A local branch; Ahead/Behind count commits relative to the checked-out branch (Merged = nothing left to merge).</summary>
public sealed record GitBranch(string Name, string Commit, string Subject, string Author, string Date, bool Current, int Ahead, int Behind, bool Merged);
