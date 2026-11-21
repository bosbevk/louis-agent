using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.SessionStream.Wire.Replies;
using LouisAgent.SessionStream.Wire.Replies.Work;

namespace LouisAgent.Core.Delegation.SubSessions;

/// <summary>
/// Reads an asked session's whole work from the server that holds it (F127.4.2), so the turns someone else had with
/// it, a visitor carrying on in the business's window, are read too; nothing when the server can't say.
/// </summary>
/// <remarks>
/// A server is asked, not trusted to answer: a session still answering (409), one the server dropped, or a server that
/// doesn't answer in time leaves the caller its own copy, what streamed through its asks.
/// </remarks>
internal sealed class SessionWorkReader(PeerHttp http, TimeSpan timeout)
{
    /// <summary>How long the server gets to say.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    /// <summary>The session's steps, oldest first; none when the server can't say.</summary>
    public async Task<IReadOnlyList<AgentWork.Step>?> ReadAsync(OpenSubSession open,
        CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        using var request = http.Request(HttpMethod.Get,
            new Uri(open.Peer, $"v1/sessions/{Uri.EscapeDataString(open.SessionId)}/work"), null);
        // Any way the server can't say leaves the caller its own copy, so the failure is the answer "none".
        try
        {
            using var response = await http.SendAsync(request, deadline.Token);
            if (!response.IsSuccessStatusCode) return null;
            var work = await response.Content.ReadFromJsonAsync<SessionWorkReply>(SessionJson.SnakeCase,
                deadline.Token);
            return work?.Steps.Select(Of).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException)
        {
            return null;
        }
    }

    /// <summary>A step from the wire as the work's own.</summary>
    /// <remarks>A switch on the kind (ADR 0051), one arm per kind.</remarks>
    private static AgentWork.Step Of(WorkStep step) => step.Kind switch
    {
        WorkStepKind.Asked => new AgentWork.Step(AgentWork.StepKind.Asked, step.Text),
        WorkStepKind.Said => new AgentWork.Step(AgentWork.StepKind.Said, step.Text),
        WorkStepKind.Called => new AgentWork.Step(AgentWork.StepKind.Called, step.Text, step.Tool),
        _ => new AgentWork.Step(AgentWork.StepKind.Returned, step.Text, step.Tool, step.Failed)
    };
}
