using LouisAgent.Api.Sessions.Streaming;

namespace LouisAgent.Api.Sessions;

/// <summary>
/// A session as the API returns it: what <c>POST /v1/sessions</c> answers and what <c>GET /v1/sessions/{id}</c> shows. Its
/// role is <c>role</c> (CL14; "personality" before), and so is each sub-session's the chat keeps open (F26).
/// </summary>
internal static class SessionView
{
    /// <summary>The answer to <c>POST /v1/sessions</c>: what the session opened with.</summary>
    public static object Created(AgentSession session) => new
    {
        session_id = session.Id, created_at = session.CreatedAt, toolsets = session.Toolsets, depth = session.Depth,
        role = session.Role, llm = session.Llm, tool = session.Tool, task = session.Tags.Task, run = session.Tags.Run,
        service = session.Tags.Service, parent = session.Tags.Parent,
    };

    /// <summary>
    /// <c>GET /v1/sessions/{id}</c>: the session now, with its tools, the specialists it keeps open by handle (never the
    /// peer's session id, F26) and its usage, like message_delta's (null when the API doesn't record usage).
    /// </summary>
    public static object Of(AgentSession session) => new
    {
        session_id = session.Id, created_at = session.CreatedAt, last_activity = session.LastActivity, busy = session.IsBusy,
        toolsets = session.Toolsets, depth = session.Depth, role = session.Role, llm = session.Llm, tool = session.Tool,
        tools = session.Engine.Tools.Count,
        task = session.Tags.Task, run = session.Tags.Run, service = session.Tags.Service,
        sub_sessions = session.SubSessions.Open.Select(o => new { handle = o.Handle, agent = o.Agent, toolsets = o.Toolsets, role = o.Role }),
        usage = session.Engine.RecordsUsage
            ? StreamFormats.Create(session.StreamFormat, session.Id, session.Toolsets, session.Role).RenderUsage(TurnUsage.From(session.Usage.Records, session.Engine.Display))
            : null,
    };
}
