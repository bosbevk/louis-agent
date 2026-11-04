namespace LouisAgent.Api.Endpoints.Git.Branches;

/// <summary>
/// The Branches tab's routes: every branch with how far it is ahead of the checked-out branch, a branch's diff,
/// merging a branch into the checked-out one, and deleting a merged branch. Branch names are only accepted if they
/// exist. Nothing here pushes; the work is <c>GitBranchesHandlers</c>'.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class GitBranchesEndpoints
{
    /// <summary>Adds the branch routes.</summary>
    public static void MapGitBranches(this IEndpointRouteBuilder routes)
    {
        var handlers = new GitBranchesHandlers();

        routes.MapGet("/git/branches", handlers.GetBranches);
        routes.MapGet("/git/branches/{**name}", handlers.GetBranch);
        routes.MapPost("/git/merge", handlers.Merge);
        // Only merged branches: git branch -d refuses to delete unmerged work.
        routes.MapDelete("/git/branches/{**name}", handlers.DeleteBranch);
    }
}
