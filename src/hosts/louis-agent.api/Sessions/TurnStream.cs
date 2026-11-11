using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using System.Globalization;
using System.Net.ServerSentEvents;
using System.Threading.Channels;
using LouisAgent.Api.Messages;
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
    /// <summary>
    /// How much of the page the visitor is reading goes to the model (F115): enough for a site's page, not a book.
    /// </summary>
    private const int MaxPageChars = 6_000;

    /// <summary>
    /// The prompt for a message: its text, then each attachment's contents (the API can't see the caller's disk), each
    /// capped so one can't fill the context, then the page the visitor is reading, capped the same way (F115).
    /// </summary>
    public static string BuildPrompt(SendMessageRequest request)
    {
        const int maxAttachmentChars = 100_000;
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Message)) parts.Add(request.Message);
        foreach (var attachment in request.Attachments ?? [])
        {
            string content = attachment.Content ?? string.Empty;
            string body = content.Length <= maxAttachmentChars ? content : content[..maxAttachmentChars];
            parts.Add(MessageText.Format(ApiModelText.Attachment, new
            {
                attachment.Name,
                Content = body
            }) +
                      (content.Length <= maxAttachmentChars
                          ? ""
                          : MessageText.Format(ApiModelText.AttachmentTruncated, new
                          {
                              Shown = maxAttachmentChars.ToString("N0", CultureInfo.InvariantCulture),
                              Length = content.Length.ToString("N0", CultureInfo.InvariantCulture),
                          })));
        }

        if (request.Page is { } page && PageBlock(page) is { Length: > 0 } block) parts.Add(block);
        return string.Join("\n\n", parts);
    }

    /// <summary>A later message's page: the section on the visitor's screen and the address, or nothing.</summary>
    private static string InViewOnly(PageRequest page)
    {
        if (page.InView is not { Id: { Length: > 0 } id } inView) return "";
        return MessageText.Format(ApiModelText.PageInViewOnly,
            new
            {
                Url = page.Url ?? "",
                Id = id,
                Heading = inView.Heading ?? "",
                Open = OpenOf(page)
            });
    }

    /// <summary>
    /// The card open on the visitor's screen as a line (FX106), so "this" is the card they opened and not one the guide
    /// opened earlier; nothing when no card is open.
    /// </summary>
    private static string OpenOf(PageRequest page) =>
        page.Open is { Id: { Length: > 0 } id } open
            ? MessageText.Format(ApiModelText.PageCardOpen, new
            {
                Id = id,
                Heading = open.Heading ?? ""
            })
            : "";

    /// <summary>
    /// The section on the visitor's screen as a line after the areas (F118), nothing when none is named.
    /// </summary>
    private static string InViewOf(PageRequest page)
    {
        if (page.InView is not { Id: { Length: > 0 } id } inView) return "";
        return MessageText.Format(ApiModelText.PageInView,
            new
            {
                Id = id,
                Heading = inView.Heading ?? "",
                Url = page.Url ?? "",
                Open = OpenOf(page)
            }).TrimEnd();
    }

    /// <summary>
    /// The page's named places as lines of "address heading" after the text (F117; each its own address since F118,
    /// ADR 0107), nothing when the page lists none.
    /// </summary>
    private static string AreasOf(PageRequest page)
    {
        var areas = (page.Areas ?? []).Where(area => !string.IsNullOrWhiteSpace(area.Id)).ToList();
        if (areas.Count == 0) return "";
        return MessageText.Format(ApiModelText.PageAreas,
            new { List = string.Join("\n", areas.Select(area => $"{area.Id} {area.Heading}".TrimEnd())) });
    }

    /// <summary>
    /// The page's links as lines of "address words" after its areas (FX120), so the guide can open where a link on the
    /// visitor's screen leads; nothing when the page has none.
    /// </summary>
    private static string LinksOf(PageRequest page)
    {
        var links = (page.Links ?? []).Where(link => !string.IsNullOrWhiteSpace(link.Id)).ToList();
        if (links.Count == 0) return "";
        return MessageText.Format(ApiModelText.PageLinks,
            new { List = string.Join("\n", links.Select(link => $"{link.Id} {link.Heading}".TrimEnd())) });
    }

    /// <summary>
    /// The page as one block: where the visitor is, what the page says, cut with a note when it is long, its areas,
    /// its links and the section on their screen. A page without words is a later message from the same page (F118):
    /// only the
    /// section on their screen, or nothing.
    /// </summary>
    private static string PageBlock(PageRequest page)
    {
        string text = page.Text ?? string.Empty;
        if (text.Length == 0) return InViewOnly(page);
        string shown = text.Length <= MaxPageChars ? text : text[..MaxPageChars];
        string block = MessageText.Format(ApiModelText.Page,
            new
            {
                Url = page.Url ?? "",
                Title = page.Title ?? "",
                Text = shown,
                Areas = AreasOf(page),
                Links = LinksOf(page),
                InView = InViewOf(page)
            });
        if (text.Length <= MaxPageChars) return block;
        return block + MessageText.Format(ApiModelText.PageTruncated, new
        {
            Shown = MaxPageChars.ToString("N0", CultureInfo.InvariantCulture),
            Length = text.Length.ToString("N0", CultureInfo.InvariantCulture),
        });
    }

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
            // F5: a sub-session's events (StartSubSession) arrive while its tool call runs, so the next update is
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
                    AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api").Log(LogLevel.Error,
                        ApiLogMessage.TurnFailed, new { Session = session.Id }, exception: ex, tag: "API");
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
