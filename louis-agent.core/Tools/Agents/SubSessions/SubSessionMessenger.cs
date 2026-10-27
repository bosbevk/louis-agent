
using System.Diagnostics;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Tools.Agents.Messages;
using LouisAgent.Core.Usage.Records;

namespace LouisAgent.Core.Tools.Agents.SubSessions;
/// <summary>One message to a sub-session (S, CL15, from SpecialistTools): sent, read to the end, asked again once when empty, its usage rolled up.</summary>
internal sealed class SubSessionMessenger(PeerHttp http, UsageRollUp rollUp)
{
    /// <summary>What one message to a child came back with: the answer and its usage line, or the failure the model gets.</summary>
    /// <param name="RefusedStatus">The HTTP status when the peer refused the message (404: it no longer has the session).</param>
    internal sealed record SubSessionReply(string Text, string UsageLine, string? Failure, int? RefusedStatus = null);

    /// <summary>
    /// Sends <paramref name="body"/> and reads the answer; a child that used all its tool rounds can end with no text
    /// (FX11), so it is asked once more, in the same session, for the answer from what it found (its context and cached
    /// prefix stay; a new child would start over).
    /// </summary>
    public async Task<SubSessionReply> AnswerAsync(Uri peer, string sessionId, object body, SubSessionKind kind,
        string? toolUseId,
        UsageScope? scope, CancellationToken cancellationToken)
    {
        var (text, usage, failure, status) =
            await SendAsync(peer, sessionId, body, kind, toolUseId, scope, cancellationToken);
        if (failure is not null) return new SubSessionReply("", "", failure, status);
        if (string.IsNullOrWhiteSpace(text))
        {
            var (again, againUsage, againFailure, againStatus) = await SendAsync(peer, sessionId,
                new { message = MessageText.Format(SubSessionToolMessage.SubSessionAskForAnswer) }, kind, toolUseId,
                scope, cancellationToken);
            if (againFailure is not null) return new SubSessionReply("", "", againFailure, againStatus);
            text = again;
            usage = string.Join(" + ", new[] { usage, againUsage }.OfType<string>());
            if (string.IsNullOrWhiteSpace(text))
                return new SubSessionReply("", "", MessageText.Format(SubSessionToolMessage.SubSessionNoAnswer,
                    new
                    {
                        Agent = kind.Label,
                        Usage = usage.Length > 0 ? usage : MessageText.Format(SubSessionToolMessage.SubSessionNoUsage)
                    }));
        }

        return new SubSessionReply(text.Trim(),
            string.IsNullOrEmpty(usage) ? MessageText.Format(SubSessionToolMessage.SubSessionNoUsage) : usage, null);
    }

    /// <summary>
    /// Sends one message to the child and reads its reply: the answer's text and usage line, or the failure the model
    /// gets instead (refused, with the status, or an error event). A reply that reported usage, answered or failed, is
    /// rolled up into <paramref name="scope"/> and the ledger.
    /// </summary>
    private async Task<(string Text, string? Usage, string? Failure, int? RefusedStatus)> SendAsync(Uri peer,
        string sessionId, object body,
        SubSessionKind kind, string? toolUseId, UsageScope? scope, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var send = http.Request(HttpMethod.Post,
            new Uri(peer, $"v1/sessions/{Uri.EscapeDataString(sessionId)}/messages"), body);
        using var reply = await http.SendAsync(send, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!reply.IsSuccessStatusCode)
            return ("", null, MessageText.Format(SubSessionToolMessage.SubSessionRefused,
                new
                {
                    Agent = kind.Label,
                    Status = (int)reply.StatusCode,
                    Reason = PeerHttp.ErrorText(await reply.Content.ReadAsStringAsync(cancellationToken))
                }), (int)reply.StatusCode);

        var (text, usage, error) = await SubSessionStream.ReadAsync(
            await reply.Content.ReadAsStreamAsync(cancellationToken),
            (type, data) => SubSessionFeed.Report(new SubSessionEvent(toolUseId, kind.Label, type, data)),
            cancellationToken);
        // A failed reply's usage was charged too (FX6); a reply with none (a timeout before any event) leaves no record.
        if (usage is not null) rollUp.Record(scope, kind.LedgerModel, usage, stopwatch.Elapsed);
        string? line = usage?.Line();
        return error is null
            ? (text, line, null, null)
            : ("", line,
                MessageText.Format(SubSessionToolMessage.SubSessionFailed, new { Agent = kind.Label, Reason = error }),
                null);
    }
}
