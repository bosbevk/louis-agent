using LouisAgent.Composition.Messages;
using System.Globalization;
using System.Text.Json;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.SessionStream.Wire.Replies;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Core.Delegation.SubSessions;

/// <summary>
/// A sub-session's life on a peer (S, CL15, from SpecialistTools): opened with only the toolsets a task needs, sent the
/// task, read to the end, kept for a follow-up (F26) or deleted. Every failure is an answer the model can act on, never
/// a crash. Shared by the specialists (depth 1) and the minions (F30, depth 2).
/// </summary>
internal sealed class PeerSession(PeerHttp http, SubSessionMessenger messenger)
{
    /// <summary>
    /// The whole path, built once per tool class: the peer's API, the messenger and the roll-up into
    /// <paramref name="usageSink"/>.
    /// </summary>
    public static PeerSession For(HttpClient http, string? apiKey, IUsageSink? usageSink, TimeProvider time)
    {
        var peer = new PeerHttp(http, apiKey);
        return new PeerSession(peer, new SubSessionMessenger(peer, new UsageRollUp(usageSink, time)));
    }

    /// <summary>
    /// Opens a child on <paramref name="peer"/> with <paramref name="createBody"/>, sends
    /// <paramref name="messageBody"/> and reads the answer to the end; in a chat the child is kept on the registry
    /// (F26) and the answer carries its handle, else (or with <paramref name="keep"/> false: a minion) it is deleted.
    /// </summary>
    public async Task<string> OpenAsync(Uri peer, string agent, CreateSessionRequest createBody,
        SendMessageRequest messageBody,
        IReadOnlyList<string> toolsets,
        string? role, SubSessionKind kind, string? toolUseId, UsageScope? scope, TimeSpan timeout,
        CancellationToken cancellationToken, bool keep = true)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        string? sessionId = null;
        try
        {
            using var create = http.Request(HttpMethod.Post, new Uri(peer, "v1/sessions"), createBody);
            using var created = await http.SendAsync(create, timeoutSource.Token);
            string createdBody = await created.Content.ReadAsStringAsync(timeoutSource.Token);
            if (!created.IsSuccessStatusCode)
                return MessageText.Format(SubSessionToolMessage.SubSessionRefused,
                    new
                    {
                        Agent = kind.Label,
                        Status = (int)created.StatusCode,
                        Reason = PeerHttp.ErrorText(createdBody)
                    });
            // The API's reply, read typed; a peer that answers anything else is the unreadable case below.
            var opened = JsonSerializer.Deserialize<SessionCreatedReply>(createdBody, SessionJson.SnakeCase);
            sessionId = opened?.SessionId is { Length: > 0 } id ? id : throw new JsonException("no session_id");
            // The role it opened with, as the peer reports it (the one passed: CL10, a role is never derived).
            string? reported = opened.Role ?? role;
            // F34: the toolsets it loaded, as the peer reports them: a role may add its own to the ones sent.
            IReadOnlyList<string> loaded = opened.Toolsets is { Count: > 0 } reportedToolsets
                ? reportedToolsets
                : toolsets;

            var reply = await messenger.AnswerAsync(peer, sessionId, messageBody, kind, toolUseId, scope,
                timeoutSource.Token);
            if (reply.Failure is not null) return reply.Failure;
            if (keep && SubSessionRegistry.Current is { } registry)
            {
                // F26: the chat keeps the child, with its context and cached prefix, for a follow-up. The entry's
                // closer is this tool's delete, so the registry never speaks HTTP; the finally below leaves it alone.
                string kept = sessionId;
                var entry = new OpenSubSession(peer, kept, agent, loaded, reported, () => DeleteAsync(peer, kept))
                {
                    Kind = kind.Kind,
                    Llm = kind.Llm,
                    Address = kind.Address,
                    Work = new AgentWork(reply.Events)
                };
                string handle = await registry.AddAsync(entry);
                sessionId = null;
                return kind.KeptAnswer(entry with { Handle = handle }, reply);
            }

            return kind.Answer(reply);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return MessageText.Format(SubSessionToolMessage.SubSessionTimedOut,
                new
                {
                    Agent = kind.Label,
                    Minutes = timeout.TotalMinutes.ToString("0.##", CultureInfo.InvariantCulture)
                });
        }
        catch (HttpRequestException ex)
        {
            return MessageText.Format(SubSessionToolMessage.SubSessionFailed,
                new
                {
                    Agent = kind.Label,
                    Reason = ex.Message
                });
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException
                                       or FormatException)
        {
            // A peer that answers with something other than its API's JSON: an answer the model can act on, not a
            // crash.
            return MessageText.Format(SubSessionToolMessage.SubSessionUnreadable,
                new
                {
                    Agent = kind.Label,
                    Reason = ex.Message
                });
        }
        finally
        {
            // Always, success or not, unless the chat kept it (F26), so a peer never keeps a child nobody will use; its
            // own idle timeout is the fallback.
            if (sessionId is not null) await DeleteAsync(peer, sessionId);
        }
    }

    /// <summary>
    /// Sends a follow-up to a kept child. One that is still answering stays open and the model waits; one the peer no
    /// longer has is forgotten and started again by <paramref name="startOver"/>, when given; one that fails is closed.
    /// </summary>
    public async Task<string> ContinueAsync(SubSessionRegistry registry, OpenSubSession open,
        SendMessageRequest messageBody,
        SubSessionKind kind,
        string? toolUseId, UsageScope? scope, TimeSpan timeout, CancellationToken cancellationToken,
        Func<Task<string>>? startOver = null)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        bool keep = false;
        try
        {
            var reply = await messenger.AnswerAsync(open.Peer, open.SessionId, messageBody, kind, toolUseId, scope,
                timeoutSource.Token);
            if (reply.RefusedStatus == 404)
            {
                // The peer dropped it (idle, or restarted): nothing to close. The caller starts a new one if it can,
                // else the model starts over with the whole context.
                registry.Remove(open.Handle);
                if (startOver is not null) return await startOver();
                return MessageText.Format(SubSessionToolMessage.SubSessionGone,
                    new
                    {
                        open.Handle,
                        Agent = kind.Label
                    });
            }

            if (reply.RefusedStatus == 409)
            {
                // Still answering an earlier message: its turn is fine, so it stays open for the next try.
                keep = true;
                return MessageText.Format(SubSessionToolMessage.SubSessionBusy, new { Agent = kind.Label });
            }

            if (reply.Failure is not null) return reply.Failure;
            keep = true;
            // What it did for this message joins what it did before, in the chat's own copy.
            registry.Update(open.Handle, kept => kept with { Work = kept.Work.Then(reply.Events) });
            return kind.KeptAnswer(open, reply);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return MessageText.Format(SubSessionToolMessage.SubSessionTimedOut,
                new
                {
                    Agent = kind.Label,
                    Minutes = timeout.TotalMinutes.ToString("0.##", CultureInfo.InvariantCulture)
                });
        }
        catch (HttpRequestException ex)
        {
            return MessageText.Format(SubSessionToolMessage.SubSessionFailed,
                new
                {
                    Agent = kind.Label,
                    Reason = ex.Message
                });
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException
                                       or FormatException)
        {
            return MessageText.Format(SubSessionToolMessage.SubSessionUnreadable,
                new
                {
                    Agent = kind.Label,
                    Reason = ex.Message
                });
        }
        finally
        {
            // A follow-up that failed or timed out leaves the peer's turn in an unknown state: closed (the delete
            // cancels it), so the next child starts clean. Removed first, so a 404 above (already removed) closes
            // nothing.
            if (!keep && registry.Remove(open.Handle)) await open.Close();
        }
    }

    /// <summary>Deletes a child on its peer; a peer that can't be reached is left to its own idle timeout.</summary>
    private async Task DeleteAsync(Uri peer, string sessionId)
    {
        try
        {
            using var delete = http.Request(HttpMethod.Delete,
                new Uri(peer, $"v1/sessions/{Uri.EscapeDataString(sessionId)}"), null);
            using var _ = await http.SendAsync(delete, CancellationToken.None);
        }
        catch (HttpRequestException)
        {
        }
    }
}
