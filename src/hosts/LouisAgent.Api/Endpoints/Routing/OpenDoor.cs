using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using LouisAgent.Api.Config;
using LouisAgent.Composition.Messages;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Messages.Holes.Door;

namespace LouisAgent.Api.Endpoints.Routing;

/// <summary>What the door says about a call.</summary>
internal enum OpenDoorAnswer
{
    /// <summary>Not the door's business: the gate checks the key as always.</summary>
    NotMine,

    /// <summary>
    /// A visitor opening one of the open identities, under the day's cap, a call on a session the door admitted, or a
    /// call on a session the host doesn't have, which its handler answers as gone.
    /// </summary>
    Pass,

    /// <summary>A visitor past the day's cap for their address.</summary>
    Capped,
}

/// <summary>
/// The guide's door (F109.3; a list since F127.2): lets a visitor with no key and no sign-in open a session as one of
/// the identities the host names open, a few times a day from one address across them all, and lets the calls on such
/// a session through after; nothing else.
/// </summary>
/// <remarks>
/// A guard beside the key gate, not inside it: the gate asks the door first and goes on with its own check when the
/// door says it's not its business, so the key's rules stay as they were. The door marks the request it admits with
/// the identity it admitted, and the session handler narrows that request to that identity's own toolsets, which
/// keeps the choice of what a visitor may run in code (ADR 0039, rule 6: a route is closed until the code opens it).
/// Counts and admitted sessions live in memory: one replica runs, and a restart forgives the day, which is the cheap
/// side to err on. A restart also forgets the sessions themselves, so a call on a session the host doesn't have goes
/// on to its handler, which says it is gone, and not to the key's check, which would tell a visitor to enter a key
/// (FX157, Louis's pick A of three: chain of responsibility, the request handed on to the part that can answer it;
/// chosen over the gate answering every unknown session on every host, and over the page guessing from a 401).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0099-the-guide-answers-without-a-sign-in-within-a-cap.md">ADR 0099: the guide answers without a sign-in, within a cap</seealso>
public sealed class OpenDoor(OpenDoorOptions options, TimeProvider? clock = null,
    Func<string, Task<SessionHolding>>? holds = null)
{
    /// <summary>Asks the host whether it has a session; with none to ask, every session counts as there.</summary>
    private readonly Func<string, Task<SessionHolding>> _holds =
        holds ?? (_ => Task.FromResult(new SessionHolding(true, null)));

    /// <summary>
    /// The mark on a request the door admitted, holding the identity it admitted, which the session handler reads.
    /// </summary>
    internal const string Mark = "louis-agent.open-door";

    private static readonly TimeSpan Remembered = TimeSpan.FromHours(24);

    /// <summary>The day a visitor's count is for, as it ends their key: a new day starts a new count.</summary>
    private const string DayFormat = "yyyy-MM-dd";

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly ConcurrentDictionary<string, int> _opened = new(StringComparer.Ordinal);

    /// <summary>
    /// Each session the door admitted: when, and for which visitor, whose count a session it asks for joins.
    /// </summary>
    private readonly ConcurrentDictionary<string, Admitted> _admitted = new(StringComparer.Ordinal);

    /// <summary>The mark on a request the door admitted, holding the visitor it counts for.</summary>
    private const string VisitorMark = "louis-agent.open-door.visitor";

    /// <summary>Where the gate marks a request as an operator's run, not a visitor's.</summary>
    private const string OperatorMark = "louis-agent.open-door.operator";

    /// <summary>The settings the door was built with.</summary>
    public OpenDoorOptions Options { get; } = options;

    /// <summary>The identity the door admitted this request as; none for a request it didn't.</summary>
    /// <summary>Whether the door let this request through as an operator's run (F126.3.1), not a visitor's.</summary>
    internal static bool IsOperatorsRun(HttpContext context) => context.Items.ContainsKey(OperatorMark);

    internal static string? AdmittedIdentity(HttpContext context) =>
        context.Items.TryGetValue(Mark, out object? identity) ? identity as string : null;

    /// <summary>What the door says about this call.</summary>
    /// <remarks>
    /// Guard clauses: a shut door, a call on an admitted session or on one the host doesn't have, then a session opened
    /// in one of the open identities.
    /// A session an admitted session's turn asks for (its <c>parent</c>, F127.4.2) counts as that visitor's, not the
    /// address that opened it, which is the server's own: the visitor's window may carry on in it, under their cap.
    /// </remarks>
    internal async Task<OpenDoorAnswer> ConsiderAsync(HttpContext context, bool keyed = false)
    {
        if (!Options.IsOpen) return OpenDoorAnswer.NotMine;
        if (SessionIdOf(context.Request.Path) is { } id && _admitted.ContainsKey(id)) return OpenDoorAnswer.Pass;
        if (SessionIdOf(context.Request.Path) is { } named)
        {
            var holding = await _holds(named);
            // A session the host has nowhere: its handler says it is gone, which needs no key.
            if (!holding.Held) return OpenDoorAnswer.Pass;
            // A visitor's chat the door opened before a restart (F43.1): admitted again, as it was used; a chat
            // opened with the key falls through and still needs it.
            if (holding.AdmittedAs is not null)
            {
                Admit(named, context);
                return OpenDoorAnswer.Pass;
            }
        }

        if (!IsOpeningASession(context.Request)) return OpenDoorAnswer.NotMine;
        if (await OpeningAsync(context.Request) is not { Identity: { } identity } opening)
            return OpenDoorAnswer.NotMine;
        // An operator's run (F126.3.1, an eval) presents the key and names its run: it gets the identity as a visitor
        // does, but is no visitor, so nothing is counted and it keeps what it asked of the run.
        if (keyed && opening.Run is not null)
        {
            context.Items[Mark] = identity;
            context.Items[OperatorMark] = true;
            return OpenDoorAnswer.Pass;
        }

        // One more for this visitor today, unless the day's cap is reached; other days' counts go as they're met.
        string visitor = VisitorOf(opening.ParentSession) ?? AddressOf(context);
        string today = _clock.GetUtcNow().ToString(DayFormat, CultureInfo.InvariantCulture);
        string key = $"{visitor}|{today}";
        foreach (string old in _opened.Keys.Where(k => !k.EndsWith(today, StringComparison.Ordinal)))
            _opened.TryRemove(old, out _);
        int count = _opened.AddOrUpdate(key, 1, (_, n) => n + 1);
        if (count > Options.SessionsPerDay)
        {
            _opened.AddOrUpdate(key, Options.SessionsPerDay, (_, n) => n - 1);
            return OpenDoorAnswer.Capped;
        }

        context.Items[Mark] = identity;
        context.Items[VisitorMark] = visitor;
        return OpenDoorAnswer.Pass;
    }

    /// <summary>
    /// Remembers a session the door opened, and the visitor it counted for, so its own calls pass for a day.
    /// </summary>
    internal void Admit(string sessionId, HttpContext context)
    {
        var now = _clock.GetUtcNow();
        foreach (var (id, _) in _admitted.Where(entry => now - entry.Value.At > Remembered).ToList())
            _admitted.TryRemove(id, out _);
        string visitor = context.Items.TryGetValue(VisitorMark, out object? who) && who is string named
            ? named
            : AddressOf(context);
        _admitted[sessionId] = new Admitted(now, visitor);
    }

    /// <summary>The visitor an admitted session counts for; none for a session the door didn't admit.</summary>
    private string? VisitorOf(string? sessionId) =>
        sessionId is not null && _admitted.TryGetValue(sessionId, out var admitted) ? admitted.Visitor : null;

    /// <summary>The text of the 429 a capped visitor gets.</summary>
    internal string CapReachedText()
    {
        var cap = new OpenDoorCapReachedHoles(Options.SessionsPerDay);
        return MessageText.Format(ApiUserText.OpenDoorCapReached, cap);
    }

    private static bool IsOpeningASession(HttpRequest request) =>
        HttpMethods.IsPost(request.Method) && request.Path.Equals($"{ApiMap.Version}/sessions");

    /// <summary>The session id in <c>/v1/sessions/{id}…</c>, or null for any other path.</summary>
    private static string? SessionIdOf(PathString path)
    {
        var parts = (path.Value ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 && parts[0] == "v1" && parts[1] == "sessions" ? parts[2] : null;
    }

    /// <summary>
    /// What the body opens: the open identity it names, as the host names it (none when it names another or none),
    /// and the session whose turn asked for it, from its <c>parent</c> tag (<c>{session}#{turn}</c>). The body is read
    /// and rewound for the handler.
    /// </summary>
    private async Task<Opening?> OpeningAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        string text = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        try
        {
            using var json = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            if (json.RootElement.ValueKind != JsonValueKind.Object) return null;
            return new Opening(Options.Admits(Text(json.RootElement, "identity")),
                Text(json.RootElement, "parent")?.Split('#')[0], Text(json.RootElement, "run"));
        }
        catch (JsonException)
        {
            // Not JSON: the handler refuses it with its own 400, after the key's check.
            return null;
        }
    }

    /// <summary>Who is asking: the first address a proxy forwarded, else the connection's.</summary>
    private static string AddressOf(HttpContext context)
    {
        string? forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        string first = (forwarded ?? "").Split(',')[0].Trim();
        return first.Length > 0 ? first : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>A text field of the body, or none.</summary>
    private static string? Text(JsonElement body, string name) =>
        body.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>What a body opens: the open identity it names, the session whose turn asked for it, its run.</summary>
    private sealed record Opening(string? Identity, string? ParentSession, string? Run);

    /// <summary>A session the door admitted: when, and the visitor it counts for.</summary>
    private sealed record Admitted(DateTimeOffset At, string Visitor);
}
