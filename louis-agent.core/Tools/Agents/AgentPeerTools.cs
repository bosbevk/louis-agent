namespace LouisAgent.Core.Tools.Agents;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Clock;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;

/// <summary>
/// The <c>agents</c> toolset (F5-S5): opens a session on another louis-agent (a peer in AGENT_PEERS) with only the
/// toolsets a task needs, sends it the task, reads its answer to the end and closes it. Only the answer and a usage line
/// come back, so the specialist's tools and context never enter this agent's prompt. What the specialist used is rolled
/// up into this turn's totals and the ledger as one <see cref="UsagePurpose.SubSession"/> record per reply.
/// </summary>
/// <remarks>
/// The limits are in code, not in the prompt: at most <see cref="MaxPerTurn"/> sub-sessions per turn, each stopped after
/// its timeout, and the child is always deleted. A child is opened with depth 1, which the peer's API refuses the
/// <c>agents</c> toolset at, so sub-sessions are one level deep. The API key comes from AGENT_API_KEY, never from the
/// model's arguments.
/// </remarks>
public sealed class AgentPeerTools(IReadOnlyDictionary<string, Uri> peers, string? apiKey, HttpClient? http = null, TimeSpan? timeout = null,
    TimeProvider? time = null, IUsageSink? usageSink = null)
{
    /// <summary>Sub-sessions one turn may open; outside a turn (an MCP call), the most a minute may.</summary>
    public const int MaxPerTurn = 3;

    /// <summary>The window of the limit outside a turn.</summary>
    public static readonly TimeSpan NoTurnWindow = TimeSpan.FromMinutes(1);

    /// <summary>How long one sub-session may run.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    // One client for every engine: sub-session answers can take minutes, so the timeout is ours, not HttpClient's.
    private static readonly HttpClient SharedHttp = new() { Timeout = Timeout.InfiniteTimeSpan };

    private readonly HttpClient _http = http ?? SharedHttp;
    private readonly TimeSpan _timeout = timeout ?? DefaultTimeout;
    private readonly ConcurrentDictionary<string, int> _perTurn = new(StringComparer.Ordinal);
    private readonly Queue<DateTimeOffset> _noTurn = new();
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    /// <param name="usageSink">The engine's ledger, for the roll-up of each sub-session's usage (F5); null writes none.</param>
    public AgentPeerTools(AgentOptions options, IUsageSink? usageSink = null) : this(options.AgentPeers, options.AgentApiKey, usageSink: usageSink)
    {
    }

    [Description("Opens a session on another louis-agent (a peer listed in AGENT_PEERS) with only the toolsets a task needs, " +
        "sends it the task and waits for its final answer. Returns the answer and a usage line. In a chat the sub-session " +
        "stays open and the answer names its handle, so a follow-up on the same work goes to ContinueSubSession; elsewhere " +
        "it is closed. At most 3 calls per turn (starts and continues), 5 minutes each. See agents-skills.md for when to delegate.")]
    public async Task<string> StartSubSession(
        [Description("The toolsets the sub-session loads, e.g. [\"files\",\"git\",\"dotnet\"]. Never \"agents\".")] string[] toolsets,
        [Description("The whole task, self-contained: the sub-session sees nothing of this conversation.")] string message,
        // Optional, so a call without it is valid: the delegation check saw Puls leave it out (the call failed) and guess "default".
        [Description("The peer's name from AGENT_PEERS. Leave it out when this instance has one peer (the usual case); " +
            "it is then used.")] string? agent = null,
        [Description("The sub-session's personality: its role on the peer (agents-skills.md says which goes with which toolsets). " +
            "Empty: the peer's default.")] string? personality = null,
        [Description("Names of files the user attached in this conversation that the task needs; they are sent with the task " +
            "(the sub-session sees nothing else of the conversation).")] string[]? attachments = null,
        CancellationToken cancellationToken = default)
    {
        // The model names the attachments; their contents come from the session, never through the model's output.
        var available = SessionAttachments.Current;
        var forwarded = new List<object>();
        foreach (string name in attachments ?? [])
        {
            if (!available.TryGetValue(name.Trim(), out string? content))
                return MessageText.Format(ToolMessage.AttachmentUnknown, new
                {
                    Name = name, Attached = available.Count == 0 ? "(none)" : string.Join(", ", available.Keys.Order()),
                });
            forwarded.Add(new { name = name.Trim(), content });
        }

        string peerNames = peers.Count == 0 ? "(none)" : string.Join(", ", peers.Keys.Order());
        // An empty name means the only peer, so the model never has to guess one (it used to try an example name first).
        if (string.IsNullOrWhiteSpace(agent))
        {
            if (peers.Count != 1) return MessageText.Format(ToolMessage.PeerNameNeeded, new { Peers = peerNames });
            agent = peers.Keys.Single();
        }
        if (!peers.TryGetValue(agent, out Uri? peer))
            return MessageText.Format(ToolMessage.UnknownPeer, new { Agent = agent, Peers = peerNames });

        UsageScope? scope = UsageScope.Current;
        // The call that opened this sub-session, so a host can show its events under that call.
        string? toolUseId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
        string? parent = scope?.Session is { } session ? $"{session}#{scope.Turn}" : null;
        if (parent is null ? !TakeNoTurnSlot() : !TakeTurnSlot(parent))
            return MessageText.Format(parent is null ? ToolMessage.SubSessionRateLimit : ToolMessage.SubSessionLimit, new { Max = MaxPerTurn });

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        string? sessionId = null;
        try
        {
            using var create = Request(HttpMethod.Post, new Uri(peer, "sessions"), new
            {
                toolsets, depth = 1, task = $"sub-session:{agent}", run = scope?.Tags.Run, service = scope?.Tags.Service, parent,
                personality = string.IsNullOrWhiteSpace(personality) ? null : personality.Trim(),
                // The user's place, when they shared it, so the specialist's time and "near me" are theirs (F22).
                location = UserPlace.Current?.Location, time_zone = UserPlace.Current?.TimeZone,
            });
            using var created = await _http.SendAsync(create, timeout.Token);
            string createdBody = await created.Content.ReadAsStringAsync(timeout.Token);
            if (!created.IsSuccessStatusCode)
                return MessageText.Format(ToolMessage.SubSessionRefused, new { Agent = agent, Status = (int)created.StatusCode, Reason = ErrorText(createdBody) });
            using var createdJson = JsonDocument.Parse(createdBody);
            sessionId = createdJson.RootElement.GetProperty("session_id").GetString() ?? throw new JsonException("no session_id");
            // The role it opened with: the one passed, or the one the peer derived from the toolsets (roles.json).
            string? role = createdJson.RootElement.TryGetProperty("personality", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : personality;

            var (text, usage, failure) = await SendAsync(peer, sessionId, new { message, attachments = forwarded }, agent, toolUseId, scope, timeout.Token);
            if (failure is not null) return failure;
            if (string.IsNullOrWhiteSpace(text))
            {
                // A specialist that used all its tool rounds can end with no text (FX11): ask once, in the same session,
                // for the answer from what it found. Its context and cached prefix stay; a new sub-session would start over.
                var (again, againUsage, againFailure) = await SendAsync(peer, sessionId,
                    new { message = MessageText.Format(ToolMessage.SubSessionAskForAnswer) }, agent, toolUseId, scope, timeout.Token);
                if (againFailure is not null) return againFailure;
                text = again;
                usage = string.Join(" + ", new[] { usage, againUsage }.OfType<string>());
                if (string.IsNullOrWhiteSpace(text))
                    return MessageText.Format(ToolMessage.SubSessionNoAnswer, new { Agent = agent, Usage = usage.Length > 0 ? usage : MessageText.Format(ToolMessage.SubSessionNoUsage) });
            }
            string usageLine = string.IsNullOrEmpty(usage) ? MessageText.Format(ToolMessage.SubSessionNoUsage) : usage;
            if (SubSessionRegistry.Current is { } registry)
            {
                // F26: the chat keeps the specialist, with its context and cached prefix, for a follow-up. The entry's
                // closer is this tool's delete, so the registry never speaks HTTP; the finally below then leaves it alone.
                string kept = sessionId;
                string handle = await registry.AddAsync(new OpenSubSession(peer, kept, agent, toolsets, role, () => DeleteAsync(peer, kept)));
                sessionId = null;
                return MessageText.Format(ToolMessage.SubSessionAnswerKept, new
                {
                    Answer = text.Trim(), Handle = handle, Agent = agent, Toolsets = string.Join(", ", toolsets),
                    Role = string.IsNullOrWhiteSpace(role) ? MessageText.Format(ToolMessage.SubSessionDefaultRole) : role, Usage = usageLine,
                });
            }
            return MessageText.Format(ToolMessage.SubSessionAnswer, new
            {
                Answer = text.Trim(), Agent = agent, Toolsets = string.Join(", ", toolsets), Usage = usageLine,
            });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return MessageText.Format(ToolMessage.SubSessionTimedOut, new { Agent = agent, Minutes = _timeout.TotalMinutes.ToString("0.##", CultureInfo.InvariantCulture) });
        }
        catch (HttpRequestException ex)
        {
            return MessageText.Format(ToolMessage.SubSessionFailed, new { Agent = agent, Reason = ex.Message });
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            // A peer that answers with something other than its API's JSON: an answer the model can act on, not a crash.
            return MessageText.Format(ToolMessage.SubSessionUnreadable, new { Agent = agent, Reason = ex.Message });
        }
        finally
        {
            // Always, success or not, unless the chat kept it (F26), so a peer never keeps a sub-session nobody will use;
            // its own idle timeout is the fallback.
            if (sessionId is not null) await DeleteAsync(peer, sessionId);
        }
    }

    /// <summary>Deletes a sub-session on its peer; a peer that can't be reached is left to its own idle timeout.</summary>
    private async Task DeleteAsync(Uri peer, string sessionId)
    {
        try
        {
            using var delete = Request(HttpMethod.Delete, new Uri(peer, $"sessions/{Uri.EscapeDataString(sessionId)}"), null);
            using var _ = await _http.SendAsync(delete, CancellationToken.None);
        }
        catch (HttpRequestException) { }
    }

    /// <summary>
    /// Sends one message to the sub-session and reads its reply: the answer's text and usage line, or the failure the
    /// model gets instead (refused, or an error event). A reply that reported usage, answered or failed, is rolled up
    /// into <paramref name="scope"/> and the ledger.
    /// </summary>
    private async Task<(string Text, string? Usage, string? Failure)> SendAsync(Uri peer, string sessionId, object body,
        string agent, string? toolUseId, UsageScope? scope, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var send = Request(HttpMethod.Post, new Uri(peer, $"sessions/{Uri.EscapeDataString(sessionId)}/messages"), body);
        using var reply = await _http.SendAsync(send, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!reply.IsSuccessStatusCode)
            return ("", null, MessageText.Format(ToolMessage.SubSessionRefused,
                new { Agent = agent, Status = (int)reply.StatusCode, Reason = ErrorText(await reply.Content.ReadAsStringAsync(cancellationToken)) }));

        var (text, usage, error) = await ReadStreamAsync(await reply.Content.ReadAsStreamAsync(cancellationToken),
            (type, data) => SubSessionFeed.Report(new SubSessionEvent(toolUseId, agent, type, data)), cancellationToken);
        // A failed reply's usage was charged too (FX6); a reply with none (a timeout before any event) leaves no record.
        if (usage is not null) RollUp(scope, agent, usage, stopwatch.Elapsed);
        string? line = usage?.Line();
        return error is null ? (text, line, null) : ("", line, MessageText.Format(ToolMessage.SubSessionFailed, new { Agent = agent, Reason = error }));
    }

    /// <summary>
    /// One record standing for the specialist's requests on this reply, written to the ledger and added to the parent
    /// turn's scope, in that order, as <c>UsageRecordingChatClient.Store</c> does for a model request. Every view totals
    /// the scope, so the usage line, the chat total, <c>/usage</c> and the API's usage include it with no change of their
    /// own; the ledger's row keeps the child's cost attributable to this session later (its own rows are in the ledger it
    /// ran on, linked by <c>parent</c>).
    /// </summary>
    private void RollUp(UsageScope? scope, string agent, SubSessionUsage usage, TimeSpan duration)
    {
        var record = new UsageRecord(
            At: _time.GetUtcNow(),
            Round: 0,
            Purpose: UsagePurpose.SubSession,
            Host: scope?.Host,
            Session: scope?.Session,
            Turn: scope?.Turn,
            Task: scope?.Tags.Task,
            Run: scope?.Tags.Run,
            Service: scope?.Tags.Service,
            Model: $"sub-session:{agent}",
            Tokens: new UsageTokens(usage.Input, usage.CacheWrite, usage.CacheRead, usage.Output, null),
            DurationMs: (long)duration.TotalMilliseconds,
            Stop: UsagePurpose.SubSession,
            // The charged amount, in the child's ledger currency; its price table isn't sent, so none is named.
            Cost: usage.Cost is { } cost ? new UsageCost(usage.Currency ?? "USD", cost, null) : null,
            Parent: scope?.Tags.Parent,
            Requests: usage.Requests);
        // Not scope?.Add(usageSink?.Record(record)): the ledger row is written whether or not a scope is open (FX5).
        UsageRecord stored = usageSink?.Record(record) ?? record;
        scope?.Add(stored);
    }

    /// <summary>Outside a turn there's nothing to count against, so a rolling window: at most MaxPerTurn a minute.</summary>
    private bool TakeNoTurnSlot()
    {
        lock (_noTurn)
        {
            DateTimeOffset now = _time.GetUtcNow();
            while (_noTurn.Count > 0 && now - _noTurn.Peek() >= NoTurnWindow) _noTurn.Dequeue();
            if (_noTurn.Count >= MaxPerTurn) return false;
            _noTurn.Enqueue(now);
            return true;
        }
    }

    private bool TakeTurnSlot(string turn)
    {
        // Old turns' counts are dropped now and then; a turn never needs more than its few entries.
        if (_perTurn.Count > 1000) _perTurn.Clear();
        return _perTurn.AddOrUpdate(turn, 1, (_, n) => n + 1) <= MaxPerTurn;
    }

    private HttpRequestMessage Request(HttpMethod method, Uri uri, object? body)
    {
        var request = new HttpRequestMessage(method, uri);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("x-api-key", apiKey);
        return request;
    }

    private static string ErrorText(string body)
    {
        try { return JsonDocument.Parse(body).RootElement.GetProperty("error").GetProperty("message").GetString() ?? body; }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException) { return body.Length > 300 ? body[..300] : body; }
    }

    /// <summary>
    /// The answer's text, the usage from message_delta (or the error event), and the error, from the SSE stream; every
    /// event also goes to <paramref name="onEvent"/> as it arrives (the live view of the sub-session).
    /// </summary>
    private static async Task<(string Text, SubSessionUsage? Usage, string? Error)> ReadStreamAsync(Stream stream, Action<string, string> onEvent,
        CancellationToken cancellationToken)
    {
        var text = new StringBuilder();
        SubSessionUsage? usage = null;
        string? error = null, eventName = null;
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("event:", StringComparison.Ordinal)) { eventName = line[6..].Trim(); continue; }
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            string json = line[5..].Trim();
            if (eventName is not null) onEvent(eventName, json);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            switch (eventName)
            {
                case "content_block_delta" when root.GetProperty("delta") is var delta && delta.GetProperty("type").GetString() == "text_delta":
                    text.Append(delta.GetProperty("text").GetString());
                    break;
                case "message_delta" when root.TryGetProperty("usage", out var used) && used.ValueKind == JsonValueKind.Object:
                    usage = SubSessionUsage.From(used);
                    break;
                case "error":
                    error = root.GetProperty("error").GetProperty("message").GetString();
                    if (root.TryGetProperty("usage", out var failed) && failed.ValueKind == JsonValueKind.Object) usage = SubSessionUsage.From(failed);
                    break;
            }
        }
        return (text.ToString(), usage, error);
    }

    /// <summary>
    /// What one reply of a sub-session reported it used: the API's usage object on <c>message_delta</c> (or on the
    /// <c>error</c> event, FX6), with the four token kinds, how many requests they came from, and the charged cost in the
    /// child's ledger currency (never <c>display_cost</c>, which is converted for showing).
    /// </summary>
    /// <param name="Requests">Null when the peer didn't report a count (then the roll-up counts as one request).</param>
    private sealed record SubSessionUsage(long Input, long CacheWrite, long CacheRead, long Output, int? Requests, decimal? Cost, string? Currency)
    {
        public static SubSessionUsage From(JsonElement usage)
        {
            long Count(string name) => usage.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;
            long requests = Count("requests");
            return new(
                Count("input_tokens"), Count("cache_creation_input_tokens"), Count("cache_read_input_tokens"), Count("output_tokens"),
                requests > 0 ? (int)requests : null,
                usage.TryGetProperty("cost", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetDecimal() : null,
                usage.TryGetProperty("currency", out var cur) && cur.ValueKind == JsonValueKind.String ? cur.GetString() : null);
        }

        /// <summary>The usage line in the tool result: <c>3 requests, 10,200 tokens, 0.0123 USD</c>.</summary>
        public string Line()
        {
            string cost = Cost is { } amount
                ? $"{amount.ToString("0.####", CultureInfo.InvariantCulture)} {Currency ?? ""}".Trim()
                : MessageText.Format(ToolMessage.SubSessionCostUnknown);
            return MessageText.Format(ToolMessage.SubSessionUsage, new
            {
                Requests = Requests ?? 0,
                Tokens = (Input + CacheWrite + CacheRead + Output).ToString("N0", CultureInfo.InvariantCulture),
                Cost = cost,
            });
        }
    }
}