using System.Net.ServerSentEvents;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LouisAgent.Api;
using LouisAgent.Api.Config;
using LouisAgent.Api.Services.Benchmarks;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;
using LouisAgent.Api.Endpoints;
using LouisAgent.Core.Pricing;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Tools.Workspace;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Guardrails;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Services;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;
using LouisAgent.Core.Messages;
using System.Globalization;
using System.Threading.Channels;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.Clock;

// HTTP version of the ACP server, which also serves the web app: every API reply is JSON, and a message's answer streams as Server-Sent Events shaped
// like Claude's (message_start, content_block_start/delta/stop for thinking, text, tool_use and tool_result, message_stop).
var host = AgentHost.Build();

var builder = WebApplication.CreateBuilder(args);
// host.Engine (every toolset this instance can load) serves /workspace and /git; sessions get their own (F5).
builder.Services.AddSingleton(host.Engine);
builder.Services.AddSingleton(host.Engines);
builder.Services.AddSingleton<AgentSessions>();
builder.Services.AddSingleton(new WorkspaceBoundary(host.Engine.WorkspaceRoot));
var app = builder.Build();

// The agent can run shell commands and write files, so when AGENT_API_KEY is set every /sessions, /workspace and /git
// call must present it.
// The web app's files stay public: they hold no data, and the app sends the key the user enters in its settings.
string? apiKey = Environment.GetEnvironmentVariable("AGENT_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
    AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api").Log(LogLevel.Warning, ApiLogMessage.NoApiKey);
else
    app.Use(async (context, next) =>
    {
        bool isApi = context.Request.Path.StartsWithSegments("/sessions") || context.Request.Path.StartsWithSegments("/workspace") ||
                     context.Request.Path.StartsWithSegments("/git") || context.Request.Path.StartsWithSegments("/toolsets") ||
                     context.Request.Path.StartsWithSegments("/personalities") || context.Request.Path.StartsWithSegments("/model");
        if (!isApi || HasApiKey(context.Request, apiKey))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(Error("authentication_error", MessageText.Format(ApiUserText.InvalidApiKey)));
    });

// The Blazor web app (louis-agent.web): MapStaticAssets serves its files (including _framework/, compressed), and any
// path that isn't an API route loads it, e.g. /chat/{sessionId}.
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.MapGet("/health", () => Results.Json(new { status = "ok" }));

// The price table this API's usage ledger prices with, for other hosts' PRICES_URL (the orchestrator in the demo). Public
// like /health: prices aren't secret, and a host fetches them without the API key. 404 when there is no table, so a
// caller falls back to its own file.
app.MapGet("/prices", () => host.Prices.IsEmpty
    ? Results.Json(Error("not_found_error", MessageText.Format(ApiUserText.NoPriceTable)),
        statusCode: StatusCodes.Status404NotFound)
    : Results.Json(host.Prices, PriceTable.Json));

// The exchange rates this API loaded (F14), in our rates.json format, for other hosts' RATES_URL (with
// RATES_FORMAT=rates-json; the demo's orchestrator) and the web app. Public like /prices; 404 when it has none.
app.MapGet("/rates", () => host.Rates.IsEmpty
    ? Results.Json(Error("not_found_error", MessageText.Format(ApiUserText.NoRates)),
        statusCode: StatusCodes.Status404NotFound)
    : Results.Text(host.Rates.ToJson(), "application/json"));

// Worked out once: looking a symbol up goes through every culture .NET knows.
var Symbols = new Lazy<Dictionary<string, string>>(() =>
    host.Rates.Rates.Keys.Append(host.Prices.Currency).Distinct().Order(StringComparer.Ordinal)
        .ToDictionary(code => code, code => DisplayCurrency.DistinctSymbol(code, DisplayCurrency.SymbolFor(code), host.Prices.Currency)));

// What the web app needs to show costs in another currency (F14): the server's display currency (from HOST_CULTURE or
// DISPLAY_CURRENCY; the browser's own locale is the display language, not the region) and a symbol for every currency
// in /rates, worked out here, since the browser's .NET carries little culture data. Public like /rates.
app.MapGet("/rates/display", () => Results.Json(new
{
    display_currency = host.Display.Code,
    display_symbol = host.Display.Symbol,
    @base = host.Display.Base,
    rate_as_of = host.Display.RateAsOf ?? host.Rates.AsOf,
    symbols = Symbols.Value,
}));


app.MapWorkspace();
app.MapGit();
app.MapHistory();
// F27: the admin portal, behind its own key (ADMIN_API_KEY, else AGENT_API_KEY; neither: closed); its settings in
// AdminOptions (F31). A run's script calls this API back at BENCHMARK_API_URL (the container's own port, by default).
// F31: each run is recorded into the benchmark store (BENCHMARKS_MONGO_URL), its cost joined from this instance's
// ledger; without a store no run starts. At start-up nothing runs, so a run still "running" was cut off by a restart.
var admin = AdminOptions.FromEnvironment();
var benchmarks = new BenchmarkCatalog();
var benchmarkLog = AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api.Benchmarks");
IBenchmarkStore? benchmarkStore = admin.BenchmarksMongoUrl is { } mongoUrl ? new MongoBenchmarkStore(mongoUrl, admin.BenchmarksDatabase) : null;
BenchmarkRecorder? recorder = benchmarkStore is null ? null : new BenchmarkRecorder(benchmarkStore, benchmarkLog, host.Agent.LogDirectory);
if (benchmarkStore is not null)
    _ = Task.Run(async () =>
    {
        try
        {
            int closed = await benchmarkStore.CloseOrphansAsync(DateTimeOffset.UtcNow);
            if (closed > 0) benchmarkLog.Log(LogLevel.Information, ApiLogMessage.BenchmarkOrphansClosed, new { Count = closed }, tag: "API");
        }
        catch (Exception ex)
        {
            benchmarkLog.Log(LogLevel.Warning, ApiLogMessage.BenchmarkStoreUnreachable, exception: ex, tag: "API");
        }
    });
app.MapAdmin(benchmarks, new BenchmarkRunner(benchmarks, host.Engine.WorkspaceRoot, admin.BenchmarkApiUrl), admin.AdminKey, recorder,
    check => BenchmarkSettings.Of(check, host.Llm.Provider, host.Llm.Model, host.Agent.PersonalitiesEnabled));

// The toolsets a session can load (F5-S3): names, what each is for and whether this instance has its settings. Never
// the tool definitions, so a general agent can choose without loading them.
app.MapGet("/toolsets", (AgentEngineFactory engines) => Results.Json(new
{
    @default = AgentSessions.DefaultToolsets,
    toolsets = ToolsetCatalog.All.Select(t => new
    {
        name = t.Name, description = t.Description, available = t.IsAvailable(engines.Context),
        missing_settings = t.MissingSettings(engines.Context),
    }),
}));

// The model this instance answers with (F20: Puls's model picker shows it).
app.MapGet("/model", () => Results.Json(new { provider = host.Llm.Provider, model = host.Llm.Model }));

// The personalities a session can choose (F21), per level (CL8: a session's level is its depth); default is the voice a
// level-0 session gets when it names none (CL9).
app.MapGet("/personalities", (AgentEngineFactory engines) => Results.Json(new
{
    @default = engines.DefaultVoice,
    agent = engines.AvailablePersonalities(SkillLevel.Agent),
    specialists = engines.AvailablePersonalities(SkillLevel.Specialist),
    minions = engines.AvailablePersonalities(SkillLevel.Minion),
}));

// The LLM profiles a session can name besides this instance's model (F30): null is the instance's own.
app.MapGet("/llm-profiles", (AgentEngineFactory engines) => Results.Json(new
{
    @default = (string?)null,
    profiles = engines.SessionLlms,
}));

// The body is optional: { "toolsets": [...] } chooses the session's tools for its lifetime (F5; none sent: skills only);
// "personality" its personality (F21; none sent: the default voice, CL9); "llm" the LLM profile it runs on (F30; none sent: this
// instance's model); "tool" a minion's one tool (F30; depth 2 only, with "parent" the specialist's session and turn);
// { "task", "run", "service" } tag every usage record of the session (the orchestrator sends them for a fix); "depth" is
// set by an agent opening a sub-session.
app.MapPost("/sessions", (CreateSessionRequest? request, AgentSessions sessions) =>
{
    if (request?.InvalidTag() is { } invalid)
        return Results.Json(Error("invalid_request_error",
                MessageText.Format(ApiUserText.InvalidTag, new { Tag = invalid, Max = CreateSessionRequest.MaxTagLength })),
            statusCode: StatusCodes.Status400BadRequest);
    if (!string.IsNullOrWhiteSpace(request?.TimeZone) && UserPlace.FindTimeZone(request.TimeZone) is null)
        return Results.Json(Error("invalid_request_error", MessageText.Format(ApiUserText.UnknownTimeZone, new { request.TimeZone })),
            statusCode: StatusCodes.Status400BadRequest);

    AgentSession session;
    try
    {
        session = sessions.Create(request?.Toolsets,
            request is null ? null : new UsageTags(request.Task, request.Run, request.Service, request.Parent), request?.Depth ?? 0,
            request?.Personality, request?.Place(), request?.Llm, request?.Tool);
    }
    catch (Exception ex) when (ex is ToolsetException or PersonalityException or SessionLlmException)
    {
        return Results.Json(Error("invalid_request_error", ex.Message), statusCode: StatusCodes.Status400BadRequest);
    }

    return Results.Json(new
    {
        session_id = session.Id, created_at = session.CreatedAt, toolsets = session.Toolsets, depth = session.Depth,
        personality = session.Personality, llm = session.Llm, tool = session.Tool, task = session.Tags.Task, run = session.Tags.Run,
        service = session.Tags.Service, parent = session.Tags.Parent,
    }, statusCode: StatusCodes.Status201Created);
});

// usage: the session's finished turns, like message_delta's (null when the API doesn't record usage).
app.MapGet("/sessions/{id}", (string id, AgentSessions sessions) =>
    sessions.Get(id) is { } session
        ? Results.Json(new
        {
            session_id = session.Id, created_at = session.CreatedAt, last_activity = session.LastActivity, busy = session.IsBusy,
            toolsets = session.Toolsets, depth = session.Depth, personality = session.Personality, llm = session.Llm, tool = session.Tool,
            tools = session.Engine.Tools.Count,
            task = session.Tags.Task, run = session.Tags.Run, service = session.Tags.Service,
            // F26: the specialists the chat keeps open, by handle (never the peer's session id).
            sub_sessions = session.SubSessions.Open.Select(o => new { handle = o.Handle, agent = o.Agent, toolsets = o.Toolsets, personality = o.Role }),
            usage = session.Engine.RecordsUsage ? ClaudeStyleStream.Usage(session.Usage.Records, session.Engine.Display) : null,
        })
        : UnknownSession(id));

app.MapDelete("/sessions/{id}", (string id, AgentSessions sessions) =>
    sessions.Remove(id) ? Results.Json(new { session_id = id, deleted = true }) : UnknownSession(id));

app.MapPost("/sessions/{id}/cancel", (string id, AgentSessions sessions) =>
    sessions.Get(id) is { } session
        ? Results.Json(new { session_id = id, cancelled = session.Cancel() })
        : UnknownSession(id));

app.MapPost("/sessions/{id}/messages", (string id, SendMessageRequest request, AgentSessions sessions, HttpContext http) =>
{
    if (sessions.Get(id) is not { } session) return UnknownSession(id);

    string prompt = BuildPrompt(request);
    if (string.IsNullOrWhiteSpace(prompt))
        return Results.Json(Error("invalid_request_error", MessageText.Format(ApiUserText.EmptyMessage)), statusCode: StatusCodes.Status400BadRequest);

    if (!session.TryStartTurn(http.RequestAborted, out var turn))
        return Results.Json(Error("conflict_error", MessageText.Format(ApiUserText.SessionBusy, new { Id = "{id}" })),
            statusCode: StatusCodes.Status409Conflict);

    // The session keeps its attachments, so a later turn can still hand them to a specialist (F22).
    foreach (var attachment in request.Attachments ?? []) session.Remember(attachment.Name, attachment.Content);

    // Where the user is, said once, in the first message: per session, so not in the shared system prompt (F22).
    if (session.Turns == 1 && session.Place is { Location: { } location } place)
        prompt = MessageText.Format(ApiModelText.UserPlace, new { Location = location, TimeZone = place.TimeZone ?? "Africa/Johannesburg" }) + "\n\n" + prompt;

    return TypedResults.ServerSentEvents(StreamTurnAsync(session.Engine, session, prompt, turn));
});

app.Run();

static IResult UnknownSession(string id) =>
    Results.Json(Error("not_found_error", MessageText.Format(ApiUserText.UnknownSession, new { Id = id })), statusCode: StatusCodes.Status404NotFound);

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
        parts.Add(MessageText.Format(ApiModelText.Attachment, new { attachment.Name, Content = body }) +
                  (content.Length <= maxAttachmentChars ? "" : MessageText.Format(ApiModelText.AttachmentTruncated, new
                  {
                      Shown = maxAttachmentChars.ToString("N0", CultureInfo.InvariantCulture),
                      Length = content.Length.ToString("N0", CultureInfo.InvariantCulture),
                  })));
    }

    return string.Join("\n\n", parts);
}

static async IAsyncEnumerable<SseItem<string>> StreamTurnAsync(
    AgentEngine engine, AgentSession session, string prompt, CancellationTokenSource turn)
{
    var stream = new ClaudeStyleStream(session.Id);
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
                ? Error("api_error", error)
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

internal sealed record SendMessageRequest(string? Message, List<AttachmentRequest>? Attachments);

/// <summary>
/// A new session's options: its toolsets (F5), its personality (F21), its LLM profile (F30), and optional tags for the usage ledger, kept short
/// and printable. Depth is 1 when another agent opens it as a sub-session.
/// </summary>
internal sealed record CreateSessionRequest(
    string? Task, string? Run, string? Service, List<string>? Toolsets = null, int Depth = 0, string? Parent = null,
    string? Personality = null, string? Location = null,
    [property: System.Text.Json.Serialization.JsonPropertyName("time_zone")] string? TimeZone = null, string? Llm = null,
    string? Tool = null)
{
    internal const int MaxTagLength = 200;

    /// <summary>Where the user is (F22): their location when they shared it, their time zone when sent; null with neither.</summary>
    public UserPlace? Place() => string.IsNullOrWhiteSpace(Location) && string.IsNullOrWhiteSpace(TimeZone)
        ? null
        : new UserPlace(string.IsNullOrWhiteSpace(Location) ? null : Location.Trim(), string.IsNullOrWhiteSpace(TimeZone) ? null : TimeZone.Trim());

    /// <summary>The name of the first tag that is too long or holds control characters, or null when all are fine.</summary>
    public string? InvalidTag() =>
        new[] { ("task", Task), ("run", Run), ("service", Service), ("parent", Parent), ("location", Location) }
            .FirstOrDefault(t => t.Item2 is { } value && (value.Length > MaxTagLength || value.Any(char.IsControl))).Item1;
}

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

    /// <summary>
    /// A sub-session's event (F5), nested: the <c>StartSubSession</c> call it belongs to, the peer, and the peer's own event
    /// (Claude's shape), so a client can fold it into that call the way it folds the turn's own events.
    /// </summary>
    public static SseItem<string> SubSession(SubSessionEvent sub)
    {
        using var data = JsonDocument.Parse(sub.Data);
        return Event("sub_session", new
        {
            type = "sub_session", tool_use_id = sub.ToolUseId, agent = sub.Agent, @event = sub.Type, data = data.RootElement.Clone(),
        });
    }

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

    /// <param name="stopReason">end_turn, cancelled, …</param>
    /// <param name="usage">The turn's <see cref="Usage"/>; left out of the event when null (a command, or no ledger).</param>
    public IEnumerable<SseItem<string>> Stop(string stopReason, object? usage = null)
    {
        foreach (var item in CloseBlock()) yield return item;
        yield return Event("message_delta", usage is null
            ? (object)new { type = "message_delta", delta = new { stop_reason = stopReason } }
            : new { type = "message_delta", delta = new { stop_reason = stopReason }, usage });
        yield return Event("message_stop", new { type = "message_stop" });
    }

    /// <summary>
    /// What a turn (or a session) used, as the usage object on Claude's <c>message_delta</c>: the same four token fields,
    /// summed over every model request of the turn, plus our <c>requests</c>, <c>cost</c> and <c>currency</c>. The four
    /// kinds don't overlap (F1), as in Claude's own usage: <c>input_tokens</c> is the uncached input. Cost is null when
    /// no request was priced ("price unknown", never 0); <c>unpriced</c> counts the requests without a price.
    /// </summary>
    public static object Usage(IReadOnlyList<UsageRecord> records, DisplayCurrency? display = null)
    {
        UsageTotal total = UsageReport.Total("usage", records);
        string? currency = total.Cost is null ? null : records.First(r => r.Cost is not null).Cost!.Currency;
        // F14: the cost in the display currency too, beside what was charged; the same as the charged cost when not
        // converting, so a client can always show display_* and never needs the rates itself.
        bool converts = display is { Converts: true } && string.Equals(currency, display.Base, StringComparison.OrdinalIgnoreCase);
        return new
        {
            input_tokens = total.Input,
            cache_creation_input_tokens = total.CacheWrite,
            cache_read_input_tokens = total.CacheRead,
            output_tokens = total.Output,
            requests = total.Requests,
            cost = total.Cost,
            currency,
            unpriced = total.Unpriced,
            display_cost = converts ? Math.Round(total.Cost!.Value * display!.Rate, 6, MidpointRounding.AwayFromZero) : total.Cost,
            display_currency = converts ? display!.Code : currency,
            display_symbol = converts ? display!.Symbol : currency is null ? null : DisplayCurrency.SymbolFor(currency),
            rate_as_of = converts ? display!.RateAsOf : null,
        };
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
