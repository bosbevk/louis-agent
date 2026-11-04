using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Core.Messages;
using LouisAgent.SessionStream;
using LouisAgent.Orchestrator.Messages;

namespace LouisAgent.Orchestrator;

/// <summary>One tool louis-agent called while answering: its input (JSON) and the result it got back.</summary>
internal sealed class AgentToolCall(string id, string name, string input)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string Input { get; } = input;
    public string? Result { get; set; }
    public bool IsError { get; set; }
}

/// <summary>What louis-agent answered in one message: its text, the tools it called, and how the turn ended.</summary>
/// <param name="Usage">
/// What the message's model requests used and cost, from <c>message_delta.usage</c> (F3); null when louis-agent doesn't
/// record usage or the turn ended in an error.
/// </param>
/// <param name="Currency">The cost's currency; null when nothing was priced.</param>
internal sealed record AgentReply(
    string Text,
    IReadOnlyList<AgentToolCall> ToolCalls,
    string? StopReason,
    string? Error,
    UsageTotal? Usage = null,
    string? Currency = null);

/// <summary>The machine-readable line louis-agent is asked to end a fix with.</summary>
internal sealed record FixResult(
    string Status,
    string? Commit,
    string? Branch,
    string? Tests,
    string? Summary,
    string? Reason);

/// <summary>
/// Talks to louis-agent.api: one session per fix, messages streamed back as Claude-style Server-Sent Events.
/// louis-agent runs its own tool loop server-side; this client only sends the task and reads the outcome.
/// </summary>
internal sealed partial class LouisAgentClient(HttpClient http, string? apiKey)
{
    /// <param name="tags">Task, run and service for louis-agent's usage ledger, so a fix's cost can be added to its run's.</param>
    /// <param name="toolsets">The session's toolsets (F5); null lets louis-agent choose its default.</param>
    public async Task<string> CreateSessionAsync(UsageTags? tags = null, CancellationToken cancellationToken = default,
        IReadOnlyList<string>? toolsets = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/sessions");
        if (tags is not null || toolsets is not null)
            request.Content = JsonContent.Create(new
            { task = tags?.Task, run = tags?.Run, service = tags?.Service, toolsets });
        AddKey(request);
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return body.RootElement.GetProperty("session_id").GetString()!;
    }

    public async Task<AgentReply> SendAsync(string sessionId, string message, Action<string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(HttpMethod.Post, $"v1/sessions/{Uri.EscapeDataString(sessionId)}/messages")
            {
                Content = JsonContent.Create(new { message }),
            };
        AddKey(request);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new AgentReply("", [], null,
                MessageText.Format(OrchestratorToolMessage.HttpFailed,
                    new { Status = (int)response.StatusCode, Body = body }));
        }

        return await ReadStreamAsync(await response.Content.ReadAsStreamAsync(cancellationToken), onProgress,
            cancellationToken);
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"v1/sessions/{Uri.EscapeDataString(sessionId)}");
        AddKey(request);
        try
        {
            using var _ = await http.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            /* the session times out on its own */
        }
    }

    private void AddKey(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("x-api-key", apiKey);
    }

    /// <summary>Folds the event stream into a reply; <paramref name="onProgress"/> sees each tool call as it happens.</summary>
    internal static async Task<AgentReply> ReadStreamAsync(Stream stream, Action<string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var text = new StringBuilder();
        var tools = new List<AgentToolCall>();
        string? stopReason = null, error = null, currency = null;
        UsageTotal? usage = null;

        await foreach (var streamEvent in SessionStreamReader.ReadAsync(stream, cancellationToken))
            Dispatch(streamEvent);

        return new AgentReply(text.ToString(), tools, stopReason, error, usage, currency);

        void Dispatch(SessionEvent streamEvent)
        {
            switch (streamEvent)
            {
                case ContentBlockStarted started:
                    var block = started.Data.GetProperty("content_block");
                    string type = block.GetProperty("type").GetString() ?? "";
                    // Text blocks are separated by tool calls; keep them apart instead of running sentences together.
                    if (type == "text" && text.Length > 0 && !text.ToString().EndsWith('\n')) text.Append("\n\n");
                    if (type == "tool_use")
                    {
                        var call = new AgentToolCall(
                            block.GetProperty("id").GetString() ?? "",
                            block.GetProperty("name").GetString() ?? "?",
                            block.TryGetProperty("input", out var input) ? input.GetRawText() : "{}");
                        tools.Add(call);
                        onProgress?.Invoke(MessageText.Format(OrchestratorLogMessage.LouisAgentToolCall,
                            new { Tool = call.Name }));
                    }
                    else if (type == "tool_result")
                    {
                        string? id = block.TryGetProperty("tool_use_id", out var useId) ? useId.GetString() : null;
                        bool failed = block.TryGetProperty("is_error", out var isError) && isError.GetBoolean();
                        if (tools.LastOrDefault(t => t.Id == id) is { } call)
                        {
                            call.Result = block.TryGetProperty("content", out var content) ? content.GetString() : null;
                            call.IsError = failed;
                        }

                        if (failed) onProgress?.Invoke(MessageText.Format(OrchestratorLogMessage.LouisAgentToolFailed));
                    }

                    break;
                case ContentBlockDelta delta when delta.Data.GetProperty("delta") is var change
                                                  && change.GetProperty("type").GetString() == "text_delta":
                    text.Append(change.GetProperty("text").GetString());
                    break;
                case MessageDelta messageDelta:
                    stopReason = messageDelta.Data.GetProperty("delta").GetProperty("stop_reason").GetString();
                    if (messageDelta.Data.TryGetProperty("usage", out var used) &&
                        used.ValueKind == JsonValueKind.Object)
                        (usage, currency) = ReadUsage(used);
                    break;
                case ErrorReported:
                    error = streamEvent.Data.GetProperty("error").GetProperty("message").GetString();
                    // FX6: a failed reply still reports what it used, so the fix's cost isn't under-counted.
                    if (streamEvent.Data.TryGetProperty("usage", out var failedUsage) &&
                        failedUsage.ValueKind == JsonValueKind.Object)
                        (usage, currency) = ReadUsage(failedUsage);
                    break;
            }
        }
    }

    /// <summary>
    /// louis-agent.api's usage object (Claude's four token fields, plus requests, cost, currency and unpriced) as a
    /// <see cref="UsageTotal"/>, so it adds up with the triage's own records.
    /// </summary>
    internal static (UsageTotal Usage, string? Currency) ReadUsage(JsonElement usage)
    {
        long Count(string name) => usage.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt64()
            : 0;

        decimal? cost = usage.TryGetProperty("cost", out var c) && c.ValueKind == JsonValueKind.Number
            ? c.GetDecimal()
            : null;
        string? currency = usage.TryGetProperty("currency", out var cur) && cur.ValueKind == JsonValueKind.String
            ? cur.GetString()
            : null;
        return (new UsageTotal("louis-agent", (int)Count("requests"), Count("input_tokens"),
            Count("cache_creation_input_tokens"),
            Count("cache_read_input_tokens"), Count("output_tokens"), cost, (int)Count("unpriced")), currency);
    }

    /// <summary>The last FIX-RESULT line in louis-agent's answer, or null when it didn't write one.</summary>
    internal static FixResult? ParseFixResult(string text)
    {
        var match = FixResultLine().Matches(text).LastOrDefault();
        if (match is null) return null;
        string json = match.Groups[1].Value;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? Get(string name) => root.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null
                ? v.ToString()
                : null;

            return Get("status") is { } status
                ? new FixResult(status, Get("commit"), Get("branch"), Get("tests"), Get("summary"), Get("reason"))
                : null;
        }
        catch (JsonException)
        {
            // FX8: the model sometimes quotes inside a sentence without escaping ("Cher"), which isn't valid JSON. The fix
            // still happened, so read the fields from the text rather than asking louis-agent to "continue" (and pay for it).
            return ReadLoosely(json);
        }
    }

    /// <summary>
    /// The fields of a FIX-RESULT line that isn't valid JSON: the short ones (status, commit, branch, tests) are quote-free
    /// values; the free-text ones (summary, reason) run to the next field or the closing brace. Null without a status.
    /// </summary>
    private static FixResult? ReadLoosely(string json)
    {
        string? Short(string name) =>
            Regex.Match(json, $"\"{name}\"\\s*:\\s*\"([^\"\\\\]*)\"") is { Success: true } m ? m.Groups[1].Value : null;

        string? Free(string name) =>
            Regex.Match(json,
                $"\"{name}\"\\s*:\\s*\"(.*?)\"\\s*(?=,\\s*\"(?:status|commit|branch|tests|summary|reason)\"\\s*:|\\}}\\s*$)",
                RegexOptions.Singleline) is { Success: true } m
                ? m.Groups[1].Value
                : null;

        return Short("status") is { } status
            ? new FixResult(status, Short("commit"), Short("branch"), Short("tests"), Free("summary"), Free("reason"))
            : null;
    }

    [GeneratedRegex(@"FIX-RESULT:\s*`?(\{.*\})`?\s*$", RegexOptions.Multiline)]
    private static partial Regex FixResultLine();
}
