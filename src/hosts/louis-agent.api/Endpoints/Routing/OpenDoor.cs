using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using LouisAgent.Api.Config;
using LouisAgent.Composition.Messages;
using LouisAgent.Api.Messages;

namespace LouisAgent.Api.Endpoints.Routing;

/// <summary>What the door says about a call.</summary>
internal enum OpenDoorAnswer
{
    /// <summary>Not the door's business: the gate checks the key as always.</summary>
    NotMine,

    /// <summary>
    /// A visitor opening one of the open identities, under the day's cap, or a call on a session the door admitted.
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
/// side to err on.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0099-the-guide-answers-without-a-sign-in-within-a-cap.md">ADR 0099: the guide answers without a sign-in, within a cap</seealso>
public sealed class OpenDoor(OpenDoorOptions options, TimeProvider? clock = null)
{
    /// <summary>
    /// The mark on a request the door admitted, holding the identity it admitted, which the session handler reads.
    /// </summary>
    internal const string Mark = "louis-agent.open-door";

    private static readonly TimeSpan Remembered = TimeSpan.FromHours(24);

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly ConcurrentDictionary<string, int> _opened = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _admitted = new(StringComparer.Ordinal);

    /// <summary>The settings the door was built with.</summary>
    public OpenDoorOptions Options { get; } = options;

    /// <summary>The identity the door admitted this request as; none for a request it didn't.</summary>
    internal static string? AdmittedIdentity(HttpContext context) =>
        context.Items.TryGetValue(Mark, out object? identity) ? identity as string : null;

    /// <summary>What the door says about this call.</summary>
    /// <remarks>
    /// Guard clauses: a shut door, a call on an admitted session, then a visitor opening one of the open identities.
    /// </remarks>
    internal async Task<OpenDoorAnswer> ConsiderAsync(HttpContext context)
    {
        if (!Options.IsOpen) return OpenDoorAnswer.NotMine;
        if (SessionIdOf(context.Request.Path) is { } id && _admitted.ContainsKey(id)) return OpenDoorAnswer.Pass;
        if (!IsOpeningASession(context.Request)) return OpenDoorAnswer.NotMine;
        if (await AskedIdentityAsync(context.Request) is not { } identity) return OpenDoorAnswer.NotMine;

        // One more for this address today, unless the day's cap is reached; other days' counts go as they're met.
        string key = $"{AddressOf(context)}|{_clock.GetUtcNow():yyyy-MM-dd}";
        foreach (string old in _opened.Keys.Where(k => !k.EndsWith(key[^10..], StringComparison.Ordinal)))
            _opened.TryRemove(old, out _);
        int count = _opened.AddOrUpdate(key, 1, (_, n) => n + 1);
        if (count > Options.SessionsPerDay)
        {
            _opened.AddOrUpdate(key, Options.SessionsPerDay, (_, n) => n - 1);
            return OpenDoorAnswer.Capped;
        }

        context.Items[Mark] = identity;
        return OpenDoorAnswer.Pass;
    }

    /// <summary>Remembers a session the door opened, so its own calls pass for a day.</summary>
    internal void Admit(string sessionId)
    {
        var now = _clock.GetUtcNow();
        foreach (var (id, _) in _admitted.Where(entry => now - entry.Value > Remembered).ToList())
            _admitted.TryRemove(id, out _);
        _admitted[sessionId] = now;
    }

    /// <summary>The text of the 429 a capped visitor gets.</summary>
    internal string CapReachedText() =>
        MessageText.Format(ApiUserText.OpenDoorCapReached, new { Cap = Options.SessionsPerDay });

    private static bool IsOpeningASession(HttpRequest request) =>
        HttpMethods.IsPost(request.Method) && request.Path.Equals($"{ApiMap.Version}/sessions");

    /// <summary>The session id in <c>/v1/sessions/{id}…</c>, or null for any other path.</summary>
    private static string? SessionIdOf(PathString path)
    {
        var parts = (path.Value ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 && parts[0] == "v1" && parts[1] == "sessions" ? parts[2] : null;
    }

    /// <summary>
    /// The open identity the body names, as the host names it; none when it names another or none. The body is read
    /// and rewound for the handler.
    /// </summary>
    private async Task<string?> AskedIdentityAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        string text = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        try
        {
            using var json = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            if (json.RootElement.ValueKind != JsonValueKind.Object) return null;
            if (!json.RootElement.TryGetProperty("identity", out var identity)) return null;
            return identity.ValueKind == JsonValueKind.String ? Options.Admits(identity.GetString()) : null;
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
}
