using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Messages;
using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Choices;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// A chat's sessions: opened with the toolsets, role and LLM profile they keep (F5, F21, F30), read, cancelled, deleted,
/// and each message answered as a stream of Claude-style events (<see cref="TurnStream"/>).
/// </summary>
internal static class SessionEndpoints
{
    public static void MapSessions(this IEndpointRouteBuilder routes, AgentHostContext host)
    {
        // The body is optional: { "toolsets": [...] } chooses the session's tools for its lifetime (F5; none sent: skills only);
        // "role" its role (F21; none sent: the default voice, CL9); "llm" the LLM profile it runs on (F30; none sent: this
        // instance's model); "tool" a minion's one tool (F30; depth 2 only, with "parent" the specialist's session and turn);
        // { "task", "run", "service" } tag every usage record of the session (the orchestrator sends them for a fix); "depth" is
        // set by an agent opening a sub-session.
        // "replay" (F18 step 4a): what an eval session does with the web. WEB_REPLAY is its default, parsed here once, at start-up, so a
        // bad value stops the API as AGENT_AUTH does; the rules are ReplayResolution's, and each refusal is a 400 before any engine is built.
        var defaultReplay = SessionReplay.DefaultMode(host.Agent.WebReplay);
        var recordings = ReplayStore.From(host.Agent);
        routes.MapPost("/sessions", async (CreateSessionRequest? request, AgentSessions sessions) =>
        {
            if (request?.InvalidTag() is { } invalid)
                return Results.Json(Error("invalid_request_error",
                        MessageText.Format(ApiUserText.InvalidTag,
                            new { Tag = invalid, Max = CreateSessionRequest.MaxTagLength })),
                    statusCode: StatusCodes.Status400BadRequest);
            if (!string.IsNullOrWhiteSpace(request?.TimeZone) && UserPlace.FindTimeZone(request.TimeZone) is null)
                return Results.Json(
                    Error("invalid_request_error",
                        MessageText.Format(ApiUserText.UnknownTimeZone, new { request.TimeZone })),
                    statusCode: StatusCodes.Status400BadRequest);

            // "stream" names the format the session's events are written in (F18 step 1f), refused before any engine is built.
            StreamFormat? format = request is null ? StreamFormat.Claude : request.StreamFormatOrNull();
            if (format is null)
                return Results.Json(Error("invalid_request_error",
                        MessageText.Format(ApiUserText.UnknownStreamFormat,
                            new
                            {
                                Format = request!.Stream,
                                Supported = string.Join(", ", SettingChoice.Names<StreamFormat>())
                            })),
                    statusCode: StatusCodes.Status400BadRequest);

            if (!ReplayResolution.TryResolve(request, defaultReplay, host.Agent.KeepsRecordings, out var replay,
                    out string? refusal))
                return Results.Json(Error("invalid_request_error", refusal!),
                    statusCode: StatusCodes.Status400BadRequest);

            // A trial recorded again (the runner's one retry) starts afresh, so a replay sees the second attempt, not the first (F18 step 5a).
            if (recordings is not null && ReplayResolution.FreshTrial(request, replay) is { } fresh)
                await recordings.StartTrialAsync(fresh);

            AgentSession session;
            try
            {
                session = sessions.Create(request?.Toolsets,
                    request is null ? null : new UsageTags(request.Task, request.Run, request.Service, request.Parent),
                    request?.Depth ?? 0,
                    request?.RoleOrNull(), request?.Place(), request?.Llm, request?.Tool, format.Value, replay);
            }
            catch (Exception ex) when (ex is ToolsetException or RoleException or SessionLlmException)
            {
                return Results.Json(Error("invalid_request_error", ex.Message),
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return Results.Created($"{ApiMap.Version}/sessions/{session.Id}", SessionView.Created(session));
        });

        routes.MapGet("/sessions/{id}", (string id, AgentSessions sessions) =>
            sessions.Get(id) is { } session ? Results.Json(SessionView.Of(session)) : UnknownSession(id));

        routes.MapDelete("/sessions/{id}", (string id, AgentSessions sessions) =>
            sessions.Remove(id) ? Results.Json(new { session_id = id, deleted = true }) : UnknownSession(id));

        routes.MapPost("/sessions/{id}/cancel", (string id, AgentSessions sessions) =>
            sessions.Get(id) is { } session
                ? Results.Json(new { session_id = id, cancelled = session.Cancel() })
                : UnknownSession(id));

        routes.MapPost("/sessions/{id}/messages",
            (string id, SendMessageRequest request, AgentSessions sessions, HttpContext http) =>
            {
                if (sessions.Get(id) is not { } session) return UnknownSession(id);

                string prompt = TurnStream.BuildPrompt(request);
                if (string.IsNullOrWhiteSpace(prompt))
                    return Results.Json(Error("invalid_request_error", MessageText.Format(ApiUserText.EmptyMessage)),
                        statusCode: StatusCodes.Status400BadRequest);

                if (!session.TryStartTurn(http.RequestAborted, out var turn))
                    return Results.Json(
                        Error("conflict_error", MessageText.Format(ApiUserText.SessionBusy, new { Id = "{id}" })),
                        statusCode: StatusCodes.Status409Conflict);

                // The session keeps its attachments, so a later turn can still hand them to a specialist (F22).
                foreach (var attachment in request.Attachments ?? [])
                    session.Remember(attachment.Name, attachment.Content);

                // Where the user is, said once, in the first message: per session, so not in the shared system prompt (F22).
                if (session.Turns == 1 && session.Place is { Location: { } location } place)
                    prompt = MessageText.Format(ApiModelText.UserPlace,
                        new
                        {
                            Location = location,
                            TimeZone = place.TimeZone ?? host.Agent.HostTimeZone ?? TimeZoneInfo.Local.Id
                        }) + "\n\n" + prompt;

                return TypedResults.ServerSentEvents(TurnStream.StreamTurnAsync(session.Engine, session, prompt, turn));
            });
    }

    private static IResult UnknownSession(string id) =>
        Results.Json(Error("not_found_error", MessageText.Format(ApiUserText.UnknownSession, new { Id = id })),
            statusCode: StatusCodes.Status404NotFound);

    private static object Error(string type, string message) => ApiErrors.Body(type, message);
}
