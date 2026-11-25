using LouisAgent.Api.Contracts;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Messages.Holes.Sessions;
using LouisAgent.Api.Sessions;
using LouisAgent.Composition.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using MarkAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.NoContent,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Sessions;

/// <summary>
/// Lets the admin's own browser say a session it opened is theirs (FX207), so the overview's latest talks can tell
/// the admin's talks from visitors'. It sits behind the admin key, so a visitor can't mark anything.
/// </summary>
/// <remarks>Named endpoints returning <c>TypedResults</c> (ADR 0045, 0051), guard clauses first.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
internal static class AdminSessionHandlers
{
    /// <summary>Hangs the route on the admin group, so the admin key covers it.</summary>
    public static void MapAdminSessions(this RouteGroupBuilder admin) =>
        admin.MapPost("/sessions/{id}/mine", MarkMine);

    /// <summary>Marks the session as the admin's; a session the API doesn't hold is a 404.</summary>
    public static async Task<MarkAnswer> MarkMine(string id, AgentSessions sessions)
    {
        if (await sessions.GetAsync(id) is not { } session)
        {
            var unknown = new UnknownSessionHoles(id);
            return ApiErrors.NotFound(MessageText.Format(ApiUserText.UnknownSession, unknown));
        }

        session.MarkFromTheAdmin();
        return TypedResults.NoContent();
    }
}
