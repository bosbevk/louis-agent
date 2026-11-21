using System.Text.Json;
using LouisAgent.SessionStream.Wire.Replies.Work;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Sessions.Work;

/// <summary>
/// A session's work as <c>GET /v1/sessions/{id}/work</c> gives it (F127.4.2), read from the conversation the session
/// keeps: what it was asked, what it said, each call with what it sent and what came back. Its thinking and the system
/// prompt are left out, so an agent that asked it reads its work, not its mind.
/// </summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as a static extension member like the other replies' (the
/// records live in the contract, which can't see a session). A result counts as failed as the stream counts it: it
/// threw, or its text starts with "Error".
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class SessionWorkReplyFactory
{
    /// <summary>The session's work, step by step.</summary>
    extension(SessionWorkReply)
    {
        /// <summary>The steps of every turn the session had, oldest first.</summary>
        public static SessionWorkReply Of(AgentSession session) => new(session.Id, Steps(session.History));
    }

    /// <summary>Each message's steps in order, the names of the calls kept so a result can say whose it is.</summary>
    private static List<WorkStep> Steps(IEnumerable<ChatMessage> history)
    {
        var steps = new List<WorkStep>();
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var message in history.Where(message => message.Role != ChatRole.System))
            foreach (var content in message.Contents)
                if (Step(message.Role, content, names) is { } step)
                    steps.Add(step);
        return steps;
    }

    /// <summary>One piece of a message as a step; thinking and anything else is none.</summary>
    /// <remarks>A switch on the shape (ADR 0051), one arm per kind of piece.</remarks>
    private static WorkStep? Step(ChatRole role, AIContent content, Dictionary<string, string> names) =>
        content switch
        {
            TextContent { Text.Length: > 0 } text when role == ChatRole.User => new WorkStep(WorkStepKind.Asked,
                text.Text),
            TextContent { Text.Length: > 0 } text => new WorkStep(WorkStepKind.Said, text.Text),
            FunctionCallContent call => Called(call, names),
            FunctionResultContent result => Returned(result, names),
            _ => null
        };

    /// <summary>A call, its name kept for its result.</summary>
    private static WorkStep Called(FunctionCallContent call, Dictionary<string, string> names)
    {
        names[call.CallId] = call.Name;
        return new WorkStep(WorkStepKind.Called, JsonSerializer.Serialize(call.Arguments), call.Name);
    }

    /// <summary>A result, named by its call and marked when it failed.</summary>
    private static WorkStep Returned(FunctionResultContent result, Dictionary<string, string> names)
    {
        string text = result.Result?.ToString() ?? result.Exception?.Message ?? "";
        bool failed = result.Exception is not null || text.StartsWith("Error", StringComparison.OrdinalIgnoreCase);
        return new WorkStep(WorkStepKind.Returned, text, names.GetValueOrDefault(result.CallId), failed);
    }
}
