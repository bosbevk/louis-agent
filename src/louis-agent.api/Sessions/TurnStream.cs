using System.Globalization;
using System.Net.ServerSentEvents;
using System.Threading.Channels;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Sessions;

/// <summary>
/// One turn of a session as the API streams it: the prompt built from the message and its attachments, then the
/// engine's updates as Claude-style events, a sub-session's events live among them, and the turn's usage at the end.
/// </summary>
internal static class TurnStream
{
    // Attachments are sent as file contents (the API can't see the caller's disk); each is capped so one can't fill the context.
    public static string BuildPrompt(SendMessageRequest request)
    {
        const int maxAttachmentChars = 100_000;
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Message)) parts.Add(request.Message);
        foreach (var attachment in request.Attachments ?? [])
        {
            string content = attachment.Content ?? string.Empty;
            string body = content.Length <= maxAttachmentChars ? content : content[..maxAttachmentChars];
            parts.Add(MessageText.Format(ApiModelText.Attachment, new { attachment.Name, Content = body }) +
                      (content.Length <= maxAttachmentChars ? "" : MessageText.Format(ApiModelText.AttachmentTruncated, new
                      {
                          Shown = maxAttachmentChars.ToString("N0", CultureInfo.InvariantCulture),
                          Length = content.Length.ToString("N0", CultureInfo.InvariantCulture),
                      })));
        }

        return string.Join("\n\n", parts);
    }

    public static async IAsyncEnumerable<SseItem<string>> StreamTurnAsync(
        AgentEngine engine, AgentSession session, string prompt, CancellationTokenSource turn)
    {
        var stream = new ClaudeStyleStream(session.Id, session.Toolsets, session.Role);
        UsageScope? usage = null;
        try
        {
            yield return stream.MessageStart();

            // /tools, /approve, /reject and /usage are answered directly, never by the model (so they have no usage).
            if (engine.HandleUserCommand(prompt, engine.RecordsUsage ? session.Usage.Records : null) is { } commandReply)
            {
                foreach (var item in stream.Text(commandReply)) yield return item;
                foreach (var item in stream.Stop("end_turn")) yield return item;
                yield break;
            }

            // This is an async iterator, so a scope it makes current is lost at its first yield: the scope is activated
            // around each step of the engine's stream instead, which is where the model requests run.
            usage = UsageScope.Begin(host: "api", session: session.Id, turn: session.Turns, tags: session.Tags);
            await using var updates = engine.StreamPromptAsync(session.History, prompt, turn.Token).GetAsyncEnumerator(turn.Token);
            string stopReason = "end_turn";
            string? error = null;
            // F5: a sub-session's events (StartSubSession) arrive while its tool call runs, so the next update is awaited
            // alongside them and they are sent as they come: the chat shows the specialist's work live. The model still
            // gets only the sub-session's answer.
            var subSessions = Channel.CreateUnbounded<SubSessionEvent>();
            while (true)
            {
                Task<bool> next;
                using (usage.Activate())
                using (SessionAttachments.Use(session.Attachments))
                using (AgentTools.Use(session.Engine.Tools.Select(t => t.Name)))
                using (UserPlace.Use(session.Place))
                using (SubSessionRegistry.Use(session.SubSessions))
                using (SubSessionFeed.Observe(e => subSessions.Writer.TryWrite(e)))
                    next = updates.MoveNextAsync().AsTask();

                while (!next.IsCompleted)
                {
                    await Task.WhenAny(next, subSessions.Reader.WaitToReadAsync().AsTask());
                    while (subSessions.Reader.TryRead(out var sub)) yield return ClaudeStyleStream.SubSession(sub);
                }
                while (subSessions.Reader.TryRead(out var sub)) yield return ClaudeStyleStream.SubSession(sub);

                try
                {
                    if (!await next) break;
                }
                catch (OperationCanceledException)
                {
                    stopReason = "cancelled";
                    break;
                }
                catch (Exception ex)
                {
                    AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api").Log(LogLevel.Error, ApiLogMessage.TurnFailed, new { Session = session.Id }, exception: ex, tag: "API");
                    error = $"{ex.GetType().Name}: {ex.Message}";
                    break;
                }

                foreach (var content in updates.Current.Contents)
                foreach (var item in stream.Content(content))
                    yield return item;
            }

            // The stream has ended (or thrown), so every request of the turn, a cancelled or failed one too, is in the scope.
            // None when no request returned (e.g. the first one failed): there is nothing to report.
            IReadOnlyList<UsageRecord> turnRecords = usage.Records;
            object? turnUsage = engine.RecordsUsage && turnRecords.Count > 0 ? ClaudeStyleStream.Usage(turnRecords, engine.Display) : null;
            if (error is not null)
            {
                foreach (var item in stream.CloseBlock()) yield return item;
                // FX6: the requests made before the failure were charged, so the error carries the turn's usage too (an
                // extra field: Claude's error event has none, and its clients ignore what they don't know).
                yield return ClaudeStyleStream.Event("error", turnUsage is null
                    ? ApiErrors.Body("api_error", error)
                    : new { type = "error", error = new { type = "api_error", message = error }, usage = turnUsage });
            }
            else
            {
                foreach (var item in stream.Stop(stopReason, turnUsage)) yield return item;
            }
        }
        finally
        {
            // Here rather than after the loop: if the client disconnects, the SSE writer stops reading and the code after
            // the last yield never runs, but what the turn used still counts toward the session.
            if (usage is not null)
            {
                session.Usage.EndTurn(usage.Records);
                usage.Dispose();
            }
            session.EndTurn(turn);
        }
    }
}
