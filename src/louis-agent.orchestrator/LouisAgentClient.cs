using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using louis_agent.core.usage;

namespace louis_agent.orchestrator;

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
internal sealed record AgentReply(string Text, IReadOnlyList<AgentToolCall> ToolCalls, string? StopReason, string? Error);

/// <summary>The machine-readable line louis-agent is asked to end a fix with.</summary>
internal sealed record FixResult(string Status, string? Commit, string? Branch, string? Tests, string? Summary, string? Reason);

/// <summary>
/// Talks to louis-agent.api: one session per fix, messages streamed back as Claude-style Server-Sent Events.
/// louis-agent runs its own tool loop server-side; this client only sends the task and reads the outcome.
/// </summary>
internal sealed partial class LouisAgentClient(HttpClient http, string? apiKey)
{
    /// <param name="tags">Task, run and service for louis-agent's usage ledger, so a fix's cost can be added to its run's.</param>
    public async Task<string> CreateSessionAsync(UsageTags? tags = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "sessions");
        if (tags is not null) request.Content = JsonContent.Create(new { task = tags.Task, run = tags.Run, service = tags.Service });
        AddKey(request);
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return body.RootElement.GetProperty("session_id").GetString()!;
    }

    public async Task<AgentReply> SendAsync(string sessionId, string message, Action<string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"sessions/{Uri.EscapeDataString(sessionId)}/messages")
        {
            Content = JsonContent.Create(new { message }),
        };
        AddKey(request);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new AgentReply("", [], null, $"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken)}");

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
        return await ReadStreamAsync(reader, onProgress, cancellationToken);
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"sessions/{Uri.EscapeDataString(sessionId)}");
        AddKey(request);
        try { using var _ = await http.SendAsync(request); }
        catch (HttpRequestException) { /* the session times out on its own */ }
    }

    private void AddKey(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("x-api-key", apiKey);
    }

    /// <summary>Folds the event stream into a reply; <paramref name="onProgress"/> sees each tool call as it happens.</summary>
    internal static async Task<AgentReply> ReadStreamAsync(TextReader reader, Action<string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var text = new StringBuilder();
        var tools = new List<AgentToolCall>();
        string? stopReason = null, error = null, eventName = null;
        var data = new StringBuilder();

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("event:", StringComparison.Ordinal)) eventName = line[6..].Trim();
            else if (line.StartsWith("data:", StringComparison.Ordinal)) data.Append(line[5..].TrimStart());
            else if (line.Length == 0 && data.Length > 0)
            {
                Dispatch(eventName, data.ToString());
                eventName = null;
                data.Clear();
            }
        }
        if (data.Length > 0) Dispatch(eventName, data.ToString());

        return new AgentReply(text.ToString(), tools, stopReason, error);

        void Dispatch(string? name, string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            switch (name)
            {
                case "content_block_start" when root.GetProperty("content_block") is var block:
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
                        onProgress?.Invoke($"-> {call.Name}");
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
                        if (failed) onProgress?.Invoke("   (tool reported an error)");
                    }
                    break;
                case "content_block_delta" when root.GetProperty("delta") is var delta &&
                                                delta.GetProperty("type").GetString() == "text_delta":
                    text.Append(delta.GetProperty("text").GetString());
                    break;
                case "message_delta":
                    stopReason = root.GetProperty("delta").GetProperty("stop_reason").GetString();
                    break;
                case "error":
                    error = root.GetProperty("error").GetProperty("message").GetString();
                    break;
            }
        }
    }

    /// <summary>The last FIX-RESULT line in louis-agent's answer, or null when it didn't write one.</summary>
    internal static FixResult? ParseFixResult(string text)
    {
        var match = FixResultLine().Matches(text).LastOrDefault();
        if (match is null) return null;
        try
        {
            using var doc = JsonDocument.Parse(match.Groups[1].Value);
            var root = doc.RootElement;
            string? Get(string name) => root.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? v.ToString() : null;
            return Get("status") is { } status ? new FixResult(status, Get("commit"), Get("branch"), Get("tests"), Get("summary"), Get("reason")) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"FIX-RESULT:\s*`?(\{.*\})`?\s*$", RegexOptions.Multiline)]
    private static partial Regex FixResultLine();
}
