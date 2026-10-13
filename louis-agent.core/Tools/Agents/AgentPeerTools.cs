namespace LouisAgent.Core.Tools.Agents;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;

/// <summary>
/// The <c>agents</c> toolset (F5-S5): opens a session on another louis-agent (a peer in AGENT_PEERS) with only the
/// toolsets a task needs, sends it the task, reads its answer to the end and closes it. Only the answer and a usage line
/// come back, so the specialist's tools and context never enter this agent's prompt.
/// </summary>
/// <remarks>
/// The limits are in code, not in the prompt: at most <see cref="MaxPerTurn"/> sub-sessions per turn, each stopped after
/// its timeout, and the child is always deleted. A child is opened with depth 1, which the peer's API refuses the
/// <c>agents</c> toolset at, so sub-sessions are one level deep. The API key comes from AGENT_API_KEY, never from the
/// model's arguments.
/// </remarks>
public sealed class AgentPeerTools(IReadOnlyDictionary<string, Uri> peers, string? apiKey, HttpClient? http = null, TimeSpan? timeout = null)
{
    /// <summary>Sub-sessions one turn may open.</summary>
    public const int MaxPerTurn = 3;

    /// <summary>How long one sub-session may run.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    // One client for every engine: sub-session answers can take minutes, so the timeout is ours, not HttpClient's.
    private static readonly HttpClient SharedHttp = new() { Timeout = Timeout.InfiniteTimeSpan };

    private readonly HttpClient _http = http ?? SharedHttp;
    private readonly TimeSpan _timeout = timeout ?? DefaultTimeout;
    private readonly ConcurrentDictionary<string, int> _perTurn = new(StringComparer.Ordinal);

    public AgentPeerTools(AgentOptions options) : this(options.AgentPeers, options.AgentApiKey)
    {
    }

    [Description("Opens a session on another louis-agent (a peer listed in AGENT_PEERS) with only the toolsets a task needs, " +
        "sends it the task, waits for its final answer and closes the session. Returns the answer and a usage line. " +
        "At most 3 per turn, 5 minutes each. See agents-skills.md for when to delegate.")]
    public async Task<string> StartSubSession(
        [Description("The peer's name from AGENT_PEERS. Leave it empty when this instance has one peer (the usual case); " +
            "it is then used.")] string agent,
        [Description("The toolsets the sub-session loads, e.g. [\"files\",\"git\",\"dotnet\"]. Never \"agents\".")] string[] toolsets,
        [Description("The whole task, self-contained: the sub-session sees nothing of this conversation.")] string message,
        CancellationToken cancellationToken = default)
    {
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
        if (!TakeTurnSlot(parent ?? "(no turn)"))
            return MessageText.Format(ToolMessage.SubSessionLimit, new { Max = MaxPerTurn });

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        string? sessionId = null;
        try
        {
            using var create = Request(HttpMethod.Post, new Uri(peer, "sessions"), new
            {
                toolsets, depth = 1, task = $"sub-session:{agent}", run = scope?.Tags.Run, service = scope?.Tags.Service, parent,
            });
            using var created = await _http.SendAsync(create, timeout.Token);
            string createdBody = await created.Content.ReadAsStringAsync(timeout.Token);
            if (!created.IsSuccessStatusCode)
                return MessageText.Format(ToolMessage.SubSessionRefused, new { Agent = agent, Status = (int)created.StatusCode, Reason = ErrorText(createdBody) });
            sessionId = JsonDocument.Parse(createdBody).RootElement.GetProperty("session_id").GetString();

            using var send = Request(HttpMethod.Post, new Uri(peer, $"sessions/{Uri.EscapeDataString(sessionId!)}/messages"), new { message });
            using var reply = await _http.SendAsync(send, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!reply.IsSuccessStatusCode)
                return MessageText.Format(ToolMessage.SubSessionRefused,
                    new { Agent = agent, Status = (int)reply.StatusCode, Reason = ErrorText(await reply.Content.ReadAsStringAsync(timeout.Token)) });

            var (text, usage, error) = await ReadStreamAsync(await reply.Content.ReadAsStreamAsync(timeout.Token),
                (type, data) => SubSessionFeed.Report(new SubSessionEvent(toolUseId, agent, type, data)), timeout.Token);
            if (error is not null)
                return MessageText.Format(ToolMessage.SubSessionFailed, new { Agent = agent, Reason = error });
            return MessageText.Format(ToolMessage.SubSessionAnswer, new
            {
                Answer = text.Trim(), Agent = agent, Toolsets = string.Join(", ", toolsets), Usage = usage ?? MessageText.Format(ToolMessage.SubSessionNoUsage),
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
        finally
        {
            // Always, success or not, so a peer never keeps a sub-session; its own idle timeout is the fallback.
            if (sessionId is not null)
            {
                try
                {
                    using var delete = Request(HttpMethod.Delete, new Uri(peer, $"sessions/{Uri.EscapeDataString(sessionId)}"), null);
                    using var _ = await _http.SendAsync(delete, CancellationToken.None);
                }
                catch (HttpRequestException) { }
            }
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
    /// The answer's text, a usage line from message_delta (or the error event), and the error, from the SSE stream; every
    /// event also goes to <paramref name="onEvent"/> as it arrives (the live view of the sub-session).
    /// </summary>
    private static async Task<(string Text, string? Usage, string? Error)> ReadStreamAsync(Stream stream, Action<string, string> onEvent,
        CancellationToken cancellationToken)
    {
        var text = new StringBuilder();
        string? usage = null, error = null, eventName = null;
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
                    usage = UsageLine(used);
                    break;
                case "error":
                    error = root.GetProperty("error").GetProperty("message").GetString();
                    if (root.TryGetProperty("usage", out var failed) && failed.ValueKind == JsonValueKind.Object) usage = UsageLine(failed);
                    break;
            }
        }
        return (text.ToString(), usage, error);
    }

    private static string UsageLine(JsonElement usage)
    {
        long Count(string name) => usage.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;
        string cost = usage.TryGetProperty("cost", out var c) && c.ValueKind == JsonValueKind.Number
            ? $"{c.GetDecimal().ToString("0.####", CultureInfo.InvariantCulture)} {(usage.TryGetProperty("currency", out var cur) ? cur.GetString() : "")}".Trim()
            : MessageText.Format(ToolMessage.SubSessionCostUnknown);
        return MessageText.Format(ToolMessage.SubSessionUsage, new
        {
            Requests = Count("requests"),
            Tokens = (Count("input_tokens") + Count("cache_creation_input_tokens") + Count("cache_read_input_tokens") + Count("output_tokens"))
                .ToString("N0", CultureInfo.InvariantCulture),
            Cost = cost,
        });
    }
}