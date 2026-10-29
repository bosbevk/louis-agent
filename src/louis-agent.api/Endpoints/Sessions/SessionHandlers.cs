using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Sessions;
using LouisAgent.Api.Endpoints.Answers;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Usage.Records;
using Microsoft.AspNetCore.Http.HttpResults;
using CreatedAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    LouisAgent.Api.Endpoints.Answers.CreatedJson<LouisAgent.Api.Contracts.Sessions.SessionCreatedReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using SessionAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Sessions.SessionReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using DeletedAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Sessions.SessionDeletedReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using CancelledAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Sessions.SessionCancelledReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using TurnAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.ServerSentEventsResult<string>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Sessions;

/// <summary>
/// A chat's sessions as named methods: opened with the toolsets, role and LLM profile they keep (F5, F21, F30), read,
/// cancelled, deleted, and each message answered as a stream of events in the session's format.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour: the routes share the host the agent was
/// built as, its default web replay (parsed once, at start-up, so a bad value stops the API) and its recordings, so
/// they live here and each route is a named method. Each returns <c>TypedResults</c> with its answers declared; the
/// replies are written with <c>SessionJson.SnakeCase</c>, which writes the sessions API's JSON exactly as before, and
/// opening a session answers <c>CreatedJson</c> (the unattended run's finding 2).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class SessionHandlers(AgentHostContext host)
{
    private readonly WebReplayMode _defaultReplay = SessionReplay.DefaultMode(host.Agent.WebReplay);
    private readonly ReplayStore? _recordings = ReplayStore.From(host.Agent);

    /// <summary>
    /// Opens a session. Every part of the body is optional and refused before any engine is built when it can't be
    /// what a session does: a tag too long, an unknown time zone or stream format, a replay the rules refuse, or a
    /// toolset, role or LLM profile this instance doesn't have.
    /// </summary>
    public async Task<CreatedAnswer> CreateSession(CreateSessionRequest? request, AgentSessions sessions)
    {
        if (request?.InvalidTag() is { } invalid)
            return Bad(MessageText.Format(ApiUserText.InvalidTag,
                new
                {
                    Tag = invalid,
                    Max = CreateSessionRequest.MaxTagLength
                }));
        if (!string.IsNullOrWhiteSpace(request?.TimeZone) && UserPlace.FindTimeZone(request.TimeZone) is null)
            return Bad(MessageText.Format(ApiUserText.UnknownTimeZone, new { request.TimeZone }));
        // "stream" names the format the session's events are written in (F18 step 1f).
        if (FormatOf(request) is not { } format) return UnknownFormat(request!.Stream);
        if (!ReplayResolution.TryResolve(request, _defaultReplay, host.Agent.KeepsRecordings, out var replay,
                out string? refusal))
            return Bad(refusal!);

        // A trial recorded again (the runner's one retry) starts afresh, so a replay sees the second attempt, not the
        // first (F18 step 5a).
        if (_recordings is not null && ReplayResolution.FreshTrial(request, replay) is { } fresh)
            await _recordings.StartTrialAsync(fresh);

        AgentSession session;
        try
        {
            session = sessions.Create(request?.Toolsets,
                request is null ? null : new UsageTags(request.Task, request.Run, request.Service, request.Parent),
                request?.Depth ?? 0,
                request?.RoleOrNull(), request?.Place(), request?.Llm, request?.Tool, format, replay);
        }
        catch (Exception ex) when (ex is ToolsetException or RoleException or SessionLlmException)
        {
            return Bad(ex.Message);
        }

        return new CreatedJson<SessionCreatedReply>($"{ApiMap.Version}/sessions/{session.Id}",
            SessionCreatedReply.Of(session), SessionJson.SnakeCase);
    }

    /// <summary>The session now; 404 for one this instance doesn't have.</summary>
    public SessionAnswer GetSession(string id, AgentSessions sessions)
    {
        if (sessions.Get(id) is not { } session) return UnknownSession(id);
        return TypedResults.Json(SessionReply.Of(session), SessionJson.SnakeCase);
    }

    /// <summary>Removes the session; 404 for one this instance doesn't have.</summary>
    public DeletedAnswer DeleteSession(string id, AgentSessions sessions)
    {
        if (!sessions.Remove(id)) return UnknownSession(id);
        return TypedResults.Json(new SessionDeletedReply(id, true), SessionJson.SnakeCase);
    }

    /// <summary>Cancels the session's running turn, saying whether there was one; 404 for an unknown session.</summary>
    public CancelledAnswer CancelSession(string id, AgentSessions sessions)
    {
        if (sessions.Get(id) is not { } session) return UnknownSession(id);
        return TypedResults.Json(new SessionCancelledReply(id, session.Cancel()), SessionJson.SnakeCase);
    }

    /// <summary>
    /// Answers a message as a stream of events: 400 for an empty one, 409 while the session is still answering, 404 for
    /// an unknown session.
    /// </summary>
    public TurnAnswer SendMessage(string id, SendMessageRequest request, AgentSessions sessions, HttpContext http)
    {
        if (sessions.Get(id) is not { } session) return UnknownSession(id);
        string prompt = TurnStream.BuildPrompt(request);
        if (string.IsNullOrWhiteSpace(prompt)) return Bad(MessageText.Format(ApiUserText.EmptyMessage));
        if (!session.TryStartTurn(http.RequestAborted, out var turn))
            return ApiErrors.Conflict(MessageText.Format(ApiUserText.SessionBusy, new { Id = "{id}" }));

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
    }

    /// <summary>
    /// The stream format the body names; Claude's when there is no body, none for a name it doesn't know.
    /// </summary>
    private static StreamFormat? FormatOf(CreateSessionRequest? request) =>
        request is null ? StreamFormat.Claude : request.StreamFormatOrNull();

    /// <summary>The 400 for a stream format that isn't one of ours, naming them.</summary>
    private static JsonHttpResult<ApiError> UnknownFormat(string? format) =>
        Bad(MessageText.Format(ApiUserText.UnknownStreamFormat,
            new
            {
                Format = format,
                Supported = string.Join(", ", SettingChoice.Names<StreamFormat>())
            }));

    /// <summary>The 404 for a session this instance doesn't have.</summary>
    private static JsonHttpResult<ApiError> UnknownSession(string id) =>
        ApiErrors.NotFound(MessageText.Format(ApiUserText.UnknownSession, new { Id = id }));

    /// <summary>A 400 with its reason.</summary>
    private static JsonHttpResult<ApiError> Bad(string message) => ApiErrors.BadRequest(message);
}
