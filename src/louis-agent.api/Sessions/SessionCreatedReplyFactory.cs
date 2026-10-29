using LouisAgent.Api.Contracts.Sessions;

namespace LouisAgent.Api.Sessions;

/// <summary>Builds the answer to opening a session: what it opened with.</summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as a C# 14 static extension member, as
/// <c>SessionReplyFactories</c>. It has a class of its own because a static extension member compiles to a plain
/// static method on its class: two <c>Of(AgentSession)</c>, one per record, can't share one.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class SessionCreatedReplyFactory
{
    /// <summary>The answer to opening a session.</summary>
    extension(SessionCreatedReply)
    {
        /// <summary>The reply for a session just opened.</summary>
        public static SessionCreatedReply Of(AgentSession session) => new(
            session.Id,
            session.CreatedAt,
            session.Toolsets,
            session.Depth,
            session.Role,
            session.Llm,
            session.Tool,
            session.Tags.Task,
            session.Tags.Run,
            session.Tags.Service,
            session.Tags.Parent);
    }
}
