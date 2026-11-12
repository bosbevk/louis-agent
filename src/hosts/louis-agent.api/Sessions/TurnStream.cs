using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using System.Globalization;
using System.Net.ServerSentEvents;
using System.Threading.Channels;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Messages.Holes.Turns;
using LouisAgent.Api.Sessions.Prompts;
using LouisAgent.SessionStream.Wire.Requests;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Core.Usage.Records;
using LouisAgent.SessionStream;

namespace LouisAgent.Api.Sessions;

/// <summary>
/// One turn of a session as the API streams it: the prompt built from the message and its attachments, then the
/// engine's updates as Claude-style events, a sub-session's events live among them, and the turn's usage at the end.
/// </summary>
internal static class TurnStream
{
    /// <summary>The prompt for a message: <see cref="TurnPrompt"/>'s, its words, its attachments and its page.</summary>
    public static string BuildPrompt(SendMessageRequest request) => TurnPrompt.Of(request);

    /// <summary>
    /// Runs one turn and streams it: a user's command answered at once, or the engine's updates as the session's
    /// format writes them, a sub-session's events among them, and the turn's end or its failure with its kind.
    /// </summary>
    public static async IAsyncEnumerable<SseItem<string>> StreamTurnAsync(
        AgentEngine engine, AgentSession session, string prompt, CancellationTokenSource turn)
    {
        var stream = StreamFormats.Create(session.StreamFormat, session.Id, session.Toolsets, session.Role);
        UsageScope? usage = null;
        try
        {
            yield return stream.Start();

            // /tools, /approve, /reject and /usage are answered directly, never by the model (so they have no usage).
            if (engine.HandleUserCommand(prompt, engine.RecordsUsage ? session.Usage.Records : null) is
                { } commandReply)
            {
                foreach (var item in stream.Text(commandReply)) yield return item;
                foreach (var item in stream.Stop("end_turn", null)) yield return item;
                yield break;
            }

            // This is an async iterator, so a scope it makes current is lost at its first yield: the scope is activated
            // around each step of the engine's stream instead, which is where the model requests run.
            usage = UsageScope.Begin(host: "api", session: session.Id, turn: session.Turns, tags: session.Tags);
            await using var updates = engine.StreamPromptAsync(session.History, prompt, turn.Token)
                .GetAsyncEnumerator(turn.Token);
            string stopReason = "end_turn";
            string? error = null;
            var errorKind = StreamErrorKind.Api;
            // F5: a sub-session's events (AskAgent) arrive while its tool call runs, so the next update is
            // awaited alongside them and they are sent as they come: the chat shows the specialist's work live. The
            // model still gets only the sub-session's answer.
            var subSessions = Channel.CreateUnbounded<SubSessionEvent>();
            while (true)
            {
                Task<bool> next;
                using (TurnAmbience.Enter(session, usage, e => subSessions.Writer.TryWrite(e)))
                    next = updates.MoveNextAsync().AsTask();

                while (!next.IsCompleted)
                {
                    await Task.WhenAny(next, subSessions.Reader.WaitToReadAsync().AsTask());
                    while (subSessions.Reader.TryRead(out var sub)) yield return stream.SubSession(sub);
                }

                while (subSessions.Reader.TryRead(out var sub)) yield return stream.SubSession(sub);

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
                    var failed = new TurnFailedHoles(session.Id);
                    AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api").Log(LogLevel.Error,
                        ApiLogMessage.TurnFailed, failed, exception: ex, tag: "API");
                    error = $"{ex.GetType().Name}: {ex.Message}";
                    // A replay with no recorded answer is streamed as its own kind, so a reader can tell it (FX26).
                    errorKind = StreamErrorKinds.Of(ex);
                    break;
                }

                foreach (var content in updates.Current.Contents)
                    foreach (var item in stream.Content(content))
                        yield return item;
            }

            // The stream has ended (or thrown), so every request of the turn, a cancelled or failed one too, is in the
            // scope. None when no request returned (e.g. the first one failed): there is nothing to report.
            IReadOnlyList<UsageRecord> turnRecords = usage.Records;
            TurnUsage? turnUsage = engine.RecordsUsage && turnRecords.Count > 0
                ? TurnUsage.From(turnRecords, engine.Display)
                : null;
            if (error is not null)
            {
                foreach (var item in stream.Fail(errorKind, error, turnUsage)) yield return item;
            }
            else
            {
                foreach (var item in stream.Stop(stopReason, turnUsage)) yield return item;
            }
        }
        finally
        {
            // Here rather than after the loop: if the client disconnects, the SSE writer stops reading and the code
            // after the last yield never runs, but what the turn used still counts toward the session.
            if (usage is not null)
            {
                session.Usage.EndTurn(usage.Records);
                usage.Dispose();
            }

            session.EndTurn(turn);
        }
    }
}
