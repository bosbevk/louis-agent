using LouisAgent.Core.Agent;

namespace LouisAgent.Api.Endpoints.Git;

/// <summary>
/// A small git interface for the web app: see what changed, read each diff, then stage, unstage or discard. git runs
/// without a shell and every path goes through the agent's workspace checks; pushing is left to the user. The work is
/// <c>GitChangesHandlers</c>'.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class GitChangesEndpoints
{
    /// <summary>Adds the changes routes.</summary>
    public static void MapGitChanges(this IEndpointRouteBuilder routes)
    {
        var handlers = new GitChangesHandlers();

        routes.MapGet("/git/status", handlers.GetStatus);
        routes.MapGet("/git/diff", handlers.GetDiff);
        routes.MapPost("/git/stage", handlers.Stage);
        routes.MapPost("/git/unstage", handlers.Unstage);
        routes.MapPost("/git/discard", handlers.Discard);
    }

    /// <summary>
    /// The status after a commit, as the commit route still answers it until that file is swept: a 201 naming the
    /// commit in Location (ADR 0039, rule 3).
    /// </summary>
    internal static async Task<IResult> StatusResultAsync(AgentEngine engine, string? committed = null,
        string? created = null)
    {
        var read = await GitStatusRead.ReadAsync(engine, committed);
        if (read.Error is { } error) return error;
        if (created is null) return GitChangesHandlers.Answer(read);
        return TypedResults.Created(created, read.Reply);
    }
}
