using System.Diagnostics;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Tools.Agents.Messages;
using LouisAgent.Core.Usage.Records;

namespace LouisAgent.Core.Tools.Agents.SubSessions;

/// <summary>
/// Sends one message to a sub-session and reads its answer to the end. If the child stops without an answer, it's
/// asked once more. What each reply used goes to the parent's turn and the ledger, never into the answer.
/// </summary>
/// <remarks>
/// Extract Class (CL15): talking to a child is one job, so <c>SpecialistTools</c> and <c>MinionTools</c> share it
/// instead of each keeping a copy of the retry and the roll-up.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
internal sealed class SubSessionMessenger(PeerHttp http, UsageRollUp rollUp)
{
    /// <summary>
    /// What one message to a child came back with: the answer, or the failure the model gets instead. No usage: it is
    /// zero on a replay, and the answer must read the same as when it was recorded (FX27).
    /// </summary>
    /// <param name="RefusedStatus">
    /// The HTTP status when the peer refused the message (404: it no longer has the session).
    /// </param>
    internal sealed record SubSessionReply(string Text, string? Failure, int? RefusedStatus = null);

    /// <summary>
    /// Sends the message and reads the answer. A child that used all its tool rounds can stop with no text (FX11), so
    /// it's asked once more in the same session, which keeps what it found and its cached prompt; a new child would
    /// start over.
    /// </summary>
    public async Task<SubSessionReply> AnswerAsync(Uri peer, string sessionId, object body, SubSessionKind kind,
        string? toolUseId,
        UsageScope? scope, CancellationToken cancellationToken)
    {
        var (text, failure, status) = await SendAsync(peer, sessionId, body, kind, toolUseId, scope, cancellationToken);
        if (failure is not null) return new SubSessionReply("", failure, status);
        if (!string.IsNullOrWhiteSpace(text)) return new SubSessionReply(text.Trim(), null);

        // No answer: ask once more, in the same session, for the answer from what it found.
        var (again, againFailure, againStatus) = await SendAsync(peer, sessionId,
            new { message = MessageText.Format(SubSessionToolMessage.SubSessionAskForAnswer) }, kind, toolUseId,
            scope, cancellationToken);
        if (againFailure is not null) return new SubSessionReply("", againFailure, againStatus);
        if (!string.IsNullOrWhiteSpace(again)) return new SubSessionReply(again.Trim(), null);

        return new SubSessionReply("",
            MessageText.Format(SubSessionToolMessage.SubSessionNoAnswer, new { Agent = kind.Label }));
    }

    /// <summary>
    /// Sends one message to the child and reads its reply: the answer's text, or the failure the model gets instead
    /// (refused, with the status, or an error event). Whatever the reply used, answered or failed, goes to the parent's
    /// turn and the ledger.
    /// </summary>
    private async Task<(string Text, string? Failure, int? RefusedStatus)> SendAsync(Uri peer, string sessionId,
        object body, SubSessionKind kind, string? toolUseId, UsageScope? scope, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var send = http.Request(HttpMethod.Post,
            new Uri(peer, $"v1/sessions/{Uri.EscapeDataString(sessionId)}/messages"), body);
        using var reply = await http.SendAsync(send, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!reply.IsSuccessStatusCode)
            return ("", MessageText.Format(SubSessionToolMessage.SubSessionRefused,
                new
                {
                    Agent = kind.Label,
                    Status = (int)reply.StatusCode,
                    Reason = PeerHttp.ErrorText(await reply.Content.ReadAsStringAsync(cancellationToken))
                }), (int)reply.StatusCode);

        // Each event goes to the host's feed as it arrives, so the chat shows the child's work live.
        var (text, usage, error) = await SubSessionStream.ReadAsync(
            await reply.Content.ReadAsStreamAsync(cancellationToken),
            (type, data) => SubSessionFeed.Report(new SubSessionEvent(toolUseId, kind.Label, type, data)),
            cancellationToken);
        // A failed reply's usage was charged too (FX6); a reply with none (a timeout before any event) leaves no
        // record.
        if (usage is not null) rollUp.Record(scope, kind.LedgerModel, usage, stopwatch.Elapsed);
        if (error is null) return (text, null, null);

        return ("", MessageText.Format(SubSessionToolMessage.SubSessionFailed, new
        {
            Agent = kind.Label,
            Reason = error
        }),
            null);
    }
}
