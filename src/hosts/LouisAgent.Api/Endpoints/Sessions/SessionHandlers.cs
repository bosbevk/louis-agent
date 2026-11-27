using System.Security.Claims;
using LouisAgent.Api.Sessions.Opening;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Logging.Content;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Logging;
using LouisAgent.Api.Contracts;
using LouisAgent.SessionStream.Wire.Replies;
using LouisAgent.Api.Endpoints.Answers;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Messages.Holes.Places;
using LouisAgent.Api.Messages.Holes.Sessions;
using LouisAgent.Api.Messages.Holes.Turns;
using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Log;
using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Api.Sessions.Work;
using LouisAgent.SessionStream.Wire.Replies.Work;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Prompting.Roles;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Core.Usage.Records;
using LouisAgent.SessionStream.Wire.Requests;
using Microsoft.AspNetCore.Http.HttpResults;
using CreatedAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    LouisAgent.Api.Endpoints.Answers.CreatedJson<LouisAgent.SessionStream.Wire.Replies.SessionCreatedReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using SessionAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.SessionStream.Wire.Replies.SessionReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using DeletedAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.SessionStream.Wire.Replies.SessionDeletedReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using CancelledAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.SessionStream.Wire.Replies.SessionCancelledReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using WorkAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.SessionStream.Wire.Replies.Work.SessionWorkReply>,
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
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class SessionHandlers(AgentHostContext host)
{
    private readonly WebReplayMode _defaultReplay = SessionReplay.DefaultMode(host.Agent.WebReplay);
    private readonly ReplayStore? _recordings = ReplayStore.From(host.Agent);

    // A line per turn beside the ledger, for the admin's overview (F127.8); none when the ledger is off.
    private readonly TurnLog _turns = new(host.Agent.UsageLedger ? host.Agent.LogDirectory : null);

    /// <summary>The API's log, for who opened which session.</summary>
    private static readonly ILogger Log = AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api.Sessions");

    /// <summary>
    /// Opens a session. Every part of the body is optional and refused before any engine is built when it can't be
    /// what a session does: a tag too long, an unknown time zone or stream format, a replay the rules refuse, or a
    /// toolset, role or LLM profile this instance doesn't have.
    /// </summary>
    public async Task<CreatedAnswer> CreateSession(CreateSessionRequest? request, AgentSessions sessions,
        ClaimsPrincipal principal, HttpContext context, OpenDoor door, AgentEngineFactory engines)
    {
        // A visitor the door admitted (F109.3) gets the open identity it admitted, and that identity's own toolsets,
        // whatever the body asked.
        string? admitted = OpenDoor.AdmittedIdentity(context);
        if (admitted is not null)
            request = NarrowToTheOpenIdentity(request, admitted, engines, OpenDoor.IsOperatorsRun(context));
        if (request?.InvalidTag() is { } invalid)
        {
            var tooLong = new InvalidTagHoles(invalid, CreateSessionRequestReading.MaxTagLength);
            return RefuseBadRequest(MessageText.Format(ApiUserText.InvalidTag, tooLong));
        }

        if (!string.IsNullOrWhiteSpace(request?.TimeZone) && UserPlace.FindTimeZone(request.TimeZone) is null)
        {
            var unknownZone = new UnknownTimeZoneHoles(request.TimeZone);
            return RefuseBadRequest(MessageText.Format(ApiUserText.UnknownTimeZone, unknownZone));
        }

        // "stream" names the format the session's events are written in (F18 step 1f).
        if (ReadFormat(request) is not { } format) return RefuseUnknownFormat(request!.Stream);
        if (!ReplayResolution.TryResolve(request, _defaultReplay, host.Agent.KeepsRecordings, out var replay,
                out string? refusal))
            return RefuseBadRequest(refusal!);

        // A trial recorded again (the runner's one retry) starts afresh, so a replay sees the second attempt, not the
        // first (F18 step 5a).
        if (_recordings is not null && ReplayResolution.FreshTrial(request, replay) is { } fresh)
            await _recordings.StartTrialAsync(fresh);

        // Who opened it, when a signed-in person did (F61.2): read from the cookie's claims, never from the body.
        var opener = SessionOpener.Of(principal);
        var tags = request is null ? null : new UsageTags(request.Task, request.Run, request.Service, request.Parent);
        var asked = new SessionRequest(request?.Toolsets, tags, request?.Depth ?? 0, request?.RoleOrNull(),
            request?.Place(), request?.Llm, request?.Tool, format, replay, opener, request?.IdentityOrNull());
        AgentSession session;
        try
        {
            session = await sessions.CreateAsync(asked, admitted);
        }
        catch (Exception ex) when (ex is ToolsetException or RoleException or SessionLlmException)
        {
            return RefuseBadRequest(ex.Message);
        }

        if (admitted is not null) door.Admit(session.Id, context);
        if (opener is not null)
        {
            var openedBy = new SessionOpenedByHoles(session.Id, LogContent.Of(opener.UserId),
                LogContent.Of(opener.TenantId));
            Log.Log(LogLevel.Information, ApiLogMessage.SessionOpenedBy, openedBy, tag: "API");
        }

        return new CreatedJson<SessionCreatedReply>($"{ApiMap.Version}/sessions/{session.Id}",
            SessionCreatedReply.Of(session), SessionJson.SnakeCase);
    }

    /// <summary>The session now; 404 for one this instance doesn't have.</summary>
    public async Task<SessionAnswer> GetSession(string id, AgentSessions sessions)
    {
        if (await sessions.GetAsync(id) is not { } session) return RefuseUnknownSession(id);
        return TypedResults.Json(SessionReply.Of(session), SessionJson.SnakeCase);
    }

    /// <summary>
    /// The session's work, step by step (F127.4.2): 404 for an unknown session, 409 while it is still answering, since
    /// its conversation is being written.
    /// </summary>
    public async Task<WorkAnswer> GetSessionWork(string id, AgentSessions sessions)
    {
        if (await sessions.GetAsync(id) is not { } session) return RefuseUnknownSession(id);
        if (session.IsBusy) return RefuseBusy(session);
        return TypedResults.Json(SessionWorkReply.Of(session), SessionJson.SnakeCase);
    }

    /// <summary>Removes the session; 404 for one this instance doesn't have.</summary>
    public async Task<DeletedAnswer> DeleteSession(string id, AgentSessions sessions)
    {
        if (!await sessions.RemoveAsync(id)) return RefuseUnknownSession(id);
        return TypedResults.Json(new SessionDeletedReply(id, true), SessionJson.SnakeCase);
    }

    /// <summary>Cancels the session's running turn, saying whether there was one; 404 for an unknown session.</summary>
    public async Task<CancelledAnswer> CancelSession(string id, AgentSessions sessions)
    {
        if (await sessions.GetAsync(id) is not { } session) return RefuseUnknownSession(id);
        return TypedResults.Json(new SessionCancelledReply(id, session.Cancel()), SessionJson.SnakeCase);
    }

    /// <summary>
    /// Answers a message as a stream of events: 400 for an empty one, 409 while the session is still answering, 404 for
    /// an unknown session.
    /// </summary>
    public async Task<TurnAnswer> SendMessage(string id, SendMessageRequest request, AgentSessions sessions,
        HttpContext http)
    {
        // A chat this instance no longer holds is read back from the store and opened again (F43.1).
        if (await sessions.GetAsync(id) is not { } session) return RefuseUnknownSession(id);
        string prompt = TurnStream.BuildPrompt(request);
        if (string.IsNullOrWhiteSpace(prompt)) return RefuseBadRequest(MessageText.Format(ApiUserText.EmptyMessage));
        // A photo or a PDF is checked and kept before the turn starts, so a refused one starts nothing (F171.1).
        if (!session.Files.TryKeep(request.Attachments ?? [], out string refusal)) return RefuseBadRequest(refusal);
        if (!session.TryStartTurn(http.RequestAborted, out var turn)) return RefuseBusy(session);

        // The agents' windows on the visitor's screen, for an ask to one of them this turn (FX134).
        session.Windows = request.Windows ?? [];
        // The page it came from, for the turn's line in the log; a message that sends none keeps the last one's.
        session.Page = request.Page ?? session.Page;
        // The session keeps its attachments, so a later turn can still hand them to a subagent (F22).
        foreach (var attachment in (request.Attachments ?? []).Where(attachment => attachment.Bytes is null))
            session.Remember(attachment.Name, attachment.Content);

        // Where the user is, said once, in the first message: per session, so not in the shared system prompt (F22).
        if (session.Turns == 1 && session.Place is { Location: { } location } place)
        {
            var where = new UserPlaceHoles(location,
                place.TimeZone ?? host.Agent.HostTimeZone ?? TimeZoneInfo.Local.Id);
            prompt = MessageText.Format(ApiModelText.UserPlace, where) + "\n\n" + prompt;
        }

        // The engine paces the words when its host turned STREAM_PACE on, and says so before the first word, so a page
        // leaves its own pacer off and the words are paced once (F156).
        if (host.Agent.StreamPace) http.Response.Headers[ApiHeaders.StreamPaced] = ApiHeaders.StreamPacedOn;

        return TypedResults.ServerSentEvents(TurnStream.StreamTurnAsync(session.Engine, session, prompt, turn, _turns,
            sessions.TurnSaveFor(session, request)));
    }

    /// <summary>
    /// The stream format the body names; Claude's when there is no body, none for a name it doesn't know.
    /// </summary>
    private static StreamFormat? ReadFormat(CreateSessionRequest? request) =>
        request is null ? StreamFormat.Claude : request.StreamFormatOrNull();

    /// <summary>The 400 for a stream format that isn't one of ours, naming them.</summary>
    private static JsonHttpResult<ApiError> RefuseUnknownFormat(string? format)
    {
        var unknown = new UnknownStreamFormatHoles(format, string.Join(", ", SettingChoice.Names<StreamFormat>()));
        return RefuseBadRequest(MessageText.Format(ApiUserText.UnknownStreamFormat, unknown));
    }

    /// <summary>The 404 for a session this instance doesn't have.</summary>
    private static JsonHttpResult<ApiError> RefuseUnknownSession(string id)
    {
        var unknown = new UnknownSessionHoles(id);
        return ApiErrors.NotFound(MessageText.Format(ApiUserText.UnknownSession, unknown));
    }

    /// <summary>
    /// The 409 for a session that is still answering, naming it in the address to cancel it; logged with how long its
    /// turn has run and whether it was cancelled, so a stuck chat can be told apart from a busy one (FX252).
    /// </summary>
    internal static JsonHttpResult<ApiError> RefuseBusy(AgentSession session)
    {
        var refused =
            new TurnRefusedBusyHoles(session.Id, session.TurnSeconds ?? 0, session.TurnCancelled ? "yes" : "no");
        Log.Log(LogLevel.Warning, ApiLogMessage.TurnRefusedBusy, refused, tag: "API");
        var busy = new SessionBusyHoles(session.Id);
        return ApiErrors.Conflict(MessageText.Format(ApiUserText.SessionBusy, busy));
    }

    /// <summary>A 400 with its reason.</summary>
    private static JsonHttpResult<ApiError> RefuseBadRequest(string message) => ApiErrors.BadRequest(message);

    /// <summary>
    /// What a visitor's session is allowed to be: the open identity the door admitted, the toolsets of its own packs
    /// and, when it may ask other agents (F127.3.1), the <c>agents</c> toolset where this instance can reach them,
    /// nothing more, tagged for the ledger; the place they shared stays, since it only tells the agent where they are.
    /// An operator's run gets the same agent, and keeps its run, its task and its recording, untagged as a visitor's.
    /// </summary>
    private static CreateSessionRequest NarrowToTheOpenIdentity(CreateSessionRequest? request, string identity,
        AgentEngineFactory engines, bool operatorsRun)
    {
        var who = engines.Catalog.Identity(identity);
        List<string> toolsets = who is null ? [] : [.. engines.Catalog.ToolsetsOf(who)];
        // Optional: an instance with no AGENT_PEERS leaves it out, and the visitor still gets the identity.
        if (who is { Calls.Count: > 0 }) toolsets.Add(AgentSessions.AgentsToolset + ToolsetCatalog.OptionalMark);
        // An operator's run (F126.3.1, an eval) keeps its run, its task and its recording, and is no visitor's session.
        if (operatorsRun)
            return new CreateSessionRequest(request?.Task, request?.Run, null, toolsets, Location: request?.Location,
                TimeZone: request?.TimeZone, Replay: request?.Replay, Identity: identity);

        return new CreateSessionRequest(null, null, OpenDoorService, toolsets, Location: request?.Location,
            TimeZone: request?.TimeZone, Identity: identity);
    }

    /// <summary>The ledger's service tag on a visitor's session, so the guide's cost can be read apart.</summary>
    internal const string OpenDoorService = "open-door";
}
