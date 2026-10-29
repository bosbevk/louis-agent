namespace LouisAgent.Api.Endpoints.Git.Commits;

/// <summary>
/// The commit routes: create a commit, read the commit log across all branches and one commit's diff. The work is
/// <c>GitCommitsHandlers</c>'.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class GitCommitsEndpoints
{
    /// <summary>Adds the commit routes.</summary>
    public static void MapGitCommits(this IEndpointRouteBuilder routes)
    {
        var handlers = new GitCommitsHandlers();

        routes.MapPost("/git/commits", handlers.Commit);
        routes.MapGet("/git/commits", handlers.GetCommits);
        routes.MapGet("/git/commits/{sha}", handlers.GetCommit);
    }
}
