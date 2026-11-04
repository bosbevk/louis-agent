using LouisAgent.Api.Contracts.Sessions;
using LouisAgent.Api.Sessions.Streaming;

namespace LouisAgent.Api.Sessions;

/// <summary>
/// Builds a session as the API returns it: what <c>POST /v1/sessions</c> answers and what <c>GET /v1/sessions/{id}</c>
/// shows. Its role is <c>role</c> (CL14; "personality" before), and so is each sub-session's the chat keeps open (F26).
/// </summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as C# 14 static extension members, for the reason the admin
/// replies give (the unattended run's finding 1): the records live in the contract, which can't see a session, so each
/// <c>Of</c> lives here beside it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class SessionReplyFactories
{
    /// <summary>A session's read carries its usage in its stream format's shape, which only the format knows.</summary>
    extension(SessionReply)
    {
        /// <summary>The session now, with its tools, the specialists it keeps open and its usage.</summary>
        public static SessionReply Of(AgentSession session) => new(
            session.Id,
            session.CreatedAt,
            session.LastActivity,
            session.IsBusy,
            session.Toolsets,
            session.Depth,
            session.Role,
            session.Llm,
            session.Tool,
            session.Engine.Tools.Count,
            session.Tags.Task,
            session.Tags.Run,
            session.Tags.Service,
            [.. session.SubSessions.Open.Select(o => new SubSessionRow(o.Handle, o.Agent, o.Toolsets, o.Role))],
            Usage(session));
    }

    /// <summary>
    /// The session's usage, like a turn's <c>message_delta</c>, rendered by its stream format; none when the API
    /// doesn't record usage.
    /// </summary>
    private static object? Usage(AgentSession session)
    {
        if (!session.Engine.RecordsUsage) return null;
        var format = StreamFormats.Create(session.StreamFormat, session.Id, session.Toolsets, session.Role);
        return format.RenderUsage(TurnUsage.From(session.Usage.Records, session.Engine.Display));
    }
}
