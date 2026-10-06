using System.Net.ServerSentEvents;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using louis_agent.api;
using louis_agent.core;
using louis_agent.core.tools;
using Microsoft.Extensions.AI;

// HTTP version of the ACP server, which also serves the web app: every API reply is JSON, and a message's answer streams as Server-Sent Events shaped
// like Claude's (message_start, content_block_start/delta/stop for thinking, text, tool_use and tool_result, message_stop).
var host = AgentHost.Build();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(host.Engine);
builder.Services.AddSingleton<AgentSessions>();
builder.Services.AddSingleton(new WorkspaceTools(host.Engine.WorkspaceRoot));
var app = builder.Build();

// The agent can run shell commands and write files, so when AGENT_API_KEY is set every /sessions, /workspace and /git
// call must present it.
// The web app's files stay public: they hold no data, and the app sends the key the user enters in its settings.
string? apiKey = Environment.GetEnvironmentVariable("AGENT_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
    Console.Error.WriteLine("[WARN] AGENT_API_KEY not set; the API accepts unauthenticated requests. Only expose it on localhost.");
else
    app.Use(async (context, next) =>
    {
        bool isApi = context.Request.Path.StartsWithSegments("/sessions") || context.Request.Path.StartsWithSegments("/workspace") ||
                     context.Request.Path.StartsWithSegments("/git");
        if (!isApi || HasApiKey(context.Request, apiKey))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(Error("authentication_error", "Missing or invalid API key (x-api-key or Authorization: Bearer)."));
    });

// The Blazor web app (louis-agent.web): MapStaticAssets serves its files (including _framework/, compressed), and any
// path that isn't an API route loads it, e.g. /chat/{sessionId}.
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.MapGet("/health", () => Results.Json(new { status = "ok" }));

app.MapWorkspace();
app.MapGit();

app.MapPost("/sessions", (AgentSessions sessions) =>
{
    var session = sessions.Create();
    return Results.Json(new { session_id = session.Id, created_at = session.CreatedAt }, statusCode: StatusCodes.Status201Created);
});

app.MapGet("/sessions/{id}", (string id, AgentSessions sessions) =>
    sessions.Get(id) is { } session
        ? Results.Json(new { session_id = session.Id, created_at = session.CreatedAt, last_activity = session.LastActivity, busy = session.IsBusy })
        : UnknownSession(id));

app.MapDelete("/sessions/{id}", (string id, AgentSessions sessions) =>
    sessions.Remove(id) ? Results.Json(new { session_id = id, deleted = true }) : UnknownSession(id));

app.MapPost("/sessions/{id}/cancel", (string id, AgentSessions sessions) =>
    sessions.Get(id) is { } session
        ? Results.Json(new { session_id = id, cancelled = session.Cancel() })
        : UnknownSession(id));

app.MapPost("/sessions/{id}/messages", (string id, SendMessageRequest request, AgentSessions sessions, AgentEngine engine, HttpContext http) =>
{
    if (sessions.Get(id) is not { } session) return UnknownSession(id);

    string prompt = BuildPrompt(request);
    if (string.IsNullOrWhiteSpace(prompt))
        return Results.Json(Error("invalid_request_error", "Send a non-empty 'message' and/or 'attachments'."), statusCode: StatusCodes.Status400BadRequest);

    if (!session.TryStartTurn(http.RequestAborted, out var turn))
        return Results.Json(Error("conflict_error", "This session is still answering a message; wait for it or POST /sessions/{id}/cancel."),
            statusCode: StatusCodes.Status409Conflict);

    return TypedResults.ServerSentEvents(StreamTurnAsync(engine, session, prompt, turn));
});

app.Run();

static IResult UnknownSession(string id) =>
    Results.Json(Error("not_found_error", $"Unknown session: {id}"), statusCode: StatusCodes.Status404NotFound);

static object Error(string type, string message) => ApiErrors.Body(type, message);

static bool HasApiKey(HttpRequest request, string expected)
{
    string presented = request.Headers["x-api-key"].FirstOrDefault()
                       ?? request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
                       ?? string.Empty;
    return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented), Encoding.UTF8.GetBytes(expected));
}

// Attachments are sent as file contents (the API can't see the caller's disk); each is capped so one can't fill the context.
static string BuildPrompt(SendMessageRequest request)
{
    const int maxAttachmentChars = 100_000;
    var parts = new List<string>();
    if (!string.IsNullOrWhiteSpace(request.Message)) parts.Add(request.Message);
    foreach (var attachment in request.Attachments ?? [])
    {
        string content = attachment.Content ?? string.Empty;
        string body = content.Length <= maxAttachmentChars ? content : content[..maxAttachmentChars];
        parts.Add($"[Attached file: {attachment.Name}]\n```\n{body}\n```" +
                  (content.Length <= maxAttachmentChars ? "" : $"\n[Attachment truncated: showing {maxAttachmentChars:N0} of {content.Length:N0} chars]"));
    }

    return string.Join("\n\n", parts);
}

static async IAsyncEnumerable<SseItem<string>> StreamTurnAsync(
    AgentEngine engine, AgentSession session, string prompt, CancellationTokenSource turn)
{
    var stream = new ClaudeStyleStream(session.Id);
    try
    {
        yield return stream.MessageStart();

        // /tools, /approve and /reject are answered directly, never by the model.
        if (engine.HandleUserCommand(prompt) is { } commandReply)
        {
            foreach (var item in stream.Text(commandReply)) yield return item;
            foreach (var item in stream.Stop("end_turn")) yield return item;
            yield break;
        }

        await using var updates = engine.StreamPromptAsync(session.History, prompt, turn.Token).GetAsyncEnumerator(turn.Token);
        string stopReason = "end_turn";
        string? error = null;
        while (true)
        {
            try
            {
                if (!await updates.MoveNextAsync()) break;
            }
            catch (OperationCanceledException)
            {
                stopReason = "cancelled";
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API] Turn failed in {session.Id}: {ex}");
                error = $"{ex.GetType().Name}: {ex.Message}";
                break;
            }

            foreach (var content in updates.Current.Contents)
            foreach (var item in stream.Content(content))
                yield return item;
        }

        if (error is not null)
        {
            foreach (var item in stream.CloseBlock()) yield return item;
            yield return ClaudeStyleStream.Event("error", Error("api_error", error));
        }
        else
        {
            foreach (var item in stream.Stop(stopReason)) yield return item;
        }
    }
    finally
    {
        session.EndTurn(turn);
    }
}

internal sealed record SendMessageRequest(string? Message, List<AttachmentRequest>? Attachments);

internal sealed record AttachmentRequest(string Name, string? Content);

/// <summary>Turns streamed agent updates into Claude-style SSE events with JSON data.</summary>
internal sealed class ClaudeStyleStream(string sessionId)
{
    // Relaxed escaping keeps quotes and backticks readable instead of '-style escapes; the data is JSON, never embedded in HTML.
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private int _index = -1;
    private string? _openBlock;

    public static SseItem<string> Event(string type, object data) => new(JsonSerializer.Serialize(data, Json), type);

    public SseItem<string> MessageStart() =>
        Event("message_start", new { type = "message_start", message = new { session_id = sessionId, role = "assistant" } });

    public IEnumerable<SseItem<string>> Content(AIContent content) => content switch
    {
        TextReasoningContent { Text.Length: > 0 } thinking => Delta("thinking", new { type = "thinking_delta", thinking = thinking.Text }),
        TextContent { Text.Length: > 0 } text => Text(text.Text),
        FunctionCallContent call => Block(new { type = "tool_use", id = call.CallId, name = call.Name, input = call.Arguments ?? new Dictionary<string, object?>() }),
        FunctionResultContent result => Block(new
        {
            type = "tool_result",
            tool_use_id = result.CallId,
            content = result.Result?.ToString() ?? result.Exception?.Message ?? string.Empty,
            is_error = result.Exception is not null ||
                       (result.Result?.ToString() ?? "").StartsWith("Error", StringComparison.OrdinalIgnoreCase),
        }),
        _ => [],
    };

    public IEnumerable<SseItem<string>> Text(string text) => Delta("text", new { type = "text_delta", text });

    public IEnumerable<SseItem<string>> Stop(string stopReason)
    {
        foreach (var item in CloseBlock()) yield return item;
        yield return Event("message_delta", new { type = "message_delta", delta = new { stop_reason = stopReason } });
        yield return Event("message_stop", new { type = "message_stop" });
    }

    public IEnumerable<SseItem<string>> CloseBlock()
    {
        if (_openBlock is null) yield break;
        _openBlock = null;
        yield return Event("content_block_stop", new { type = "content_block_stop", index = _index });
    }

    // Streamed thinking/text: consecutive deltas of the same kind share one content block.
    private IEnumerable<SseItem<string>> Delta(string blockType, object delta)
    {
        if (_openBlock != blockType)
        {
            foreach (var item in CloseBlock()) yield return item;
            _openBlock = blockType;
            object block = blockType == "thinking" ? new { type = "thinking", thinking = "" } : new { type = "text", text = "" };
            yield return Event("content_block_start", new { type = "content_block_start", index = ++_index, content_block = block });
        }

        yield return Event("content_block_delta", new { type = "content_block_delta", index = _index, delta });
    }

    // Tool calls and results arrive whole, so each is a complete block.
    private IEnumerable<SseItem<string>> Block(object block)
    {
        foreach (var item in CloseBlock()) yield return item;
        yield return Event("content_block_start", new { type = "content_block_start", index = ++_index, content_block = block });
        yield return Event("content_block_stop", new { type = "content_block_stop", index = _index });
    }
}
