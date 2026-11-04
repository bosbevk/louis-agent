using LouisAgent.Core.Hosting;

namespace LouisAgent.Api.Endpoints.Sessions;

/// <summary>
/// A chat's sessions: opened with the toolsets, role and LLM profile they keep (F5, F21, F30), read, cancelled,
/// deleted, and each message answered as a stream of events. The work is <c>SessionHandlers</c>'.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class SessionEndpoints
{
    /// <summary>Adds the session routes.</summary>
    public static void MapSessions(this IEndpointRouteBuilder routes, AgentHostContext host)
    {
        var handlers = new SessionHandlers(host);

        routes.MapPost("/sessions", handlers.CreateSession);
        routes.MapGet("/sessions/{id}", handlers.GetSession);
        routes.MapDelete("/sessions/{id}", handlers.DeleteSession);
        routes.MapPost("/sessions/{id}/cancel", handlers.CancelSession);
        routes.MapPost("/sessions/{id}/messages", handlers.SendMessage);
    }
}
