using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;
using System.Text;
using System.Text.Json;

namespace LouisAgent.Orchestrator;

/// <summary>
/// What one error event used (F3): the orchestrator's triage (its own requests), louis-agent's fix (the usage it reported
/// for each reply), and both together. A part is null when it wasn't measured: no triage ledger, or no fix requested.
/// </summary>
internal sealed record EventUsage(UsageTotal? Triage, UsageTotal? LouisAgent, UsageTotal? Total);

/// <summary>
/// One Markdown file with everything the agents said to each other: per error event, each message the orchestrator
/// sent louis-agent (verbatim), each reply (its text verbatim, plus every tool call it made), the orchestrator's own
/// checks, and a summary; then a run summary table for testing. Written as it happens, so a half-finished run is readable.
/// </summary>
/// <param name="display">The currency costs are shown in (F14); null shows them as charged. jsonl files keep what was charged.</param>
internal sealed class CommsLog(string path, DisplayCurrency? display = null)
{
    private const int MaxCellChars = 160;

    private readonly List<EventSummary> _events = [];
    private EventSummary? _current;
    private bool _checksHeaderWritten;

    // One currency per run: both agents price from the same table (the demo's orchestrator reads louis-agent's /v1/prices).
    private string _currency = "USD";

    public string Path { get; } = System.IO.Path.GetFullPath(path);

    private sealed class EventSummary(int number, ErrorEvent error)
    {
        public int Number { get; } = number;
        public ErrorEvent Error { get; } = error;
        public int Messages { get; set; }
        public int ToolCalls { get; set; }
        public FixResult? Fix { get; set; }
        public List<string> Checks { get; } = [];
        public string Outcome { get; set; } = "unknown";
        public string Reason { get; set; } = "";

        /// <summary>The usage louis-agent reported for each reply of this event's fix.</summary>
        public List<UsageTotal> LouisAgentReplies { get; } = [];
        public EventUsage Usage { get; set; } = new(null, null, null);
    }

    public void StartRun(string service, Uri louisAgentUrl, string model)
    {
        _events.Clear();
        Append($"""
            # Agent communications: {service}

            Run started {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} · louis-agent at {louisAgentUrl} · model `{model}`

            Each error below shows what the orchestrator sent louis-agent and what louis-agent replied (text verbatim;
            tool inputs and results shortened to {MaxCellChars} characters), the orchestrator's own checks, and a summary.
            The run summary is at the end.

            """);
    }

    public void StartEvent(int number, int total, ErrorEvent error)
    {
        _current = new EventSummary(number, error);
        _events.Add(_current);
        _checksHeaderWritten = false;
        Append($"""

            ---

            ## {number}/{total} · `{error.Method} {error.Arguments}` · {ShortType(error.ExceptionType)}

            Error `{error.Id}` from the service's error log:

            ```json
            {JsonSerializer.Serialize(error, Indented)}
            ```

            """);
    }

    /// <summary>A message from the orchestrator to louis-agent, exactly as sent.</summary>
    public void Sent(string sessionId, string message)
    {
        if (_current is { } e) e.Messages++;
        Append($"""
            ### Orchestrator → louis-agent · message {_current?.Messages ?? 0} · session `{sessionId}`

            ````text
            {message}
            ````

            """);
    }

    /// <summary>louis-agent's reply to the last message: its tool calls, then its text.</summary>
    public void Received(AgentReply reply, FixResult? result)
    {
        if (_current is { } e)
        {
            e.ToolCalls += reply.ToolCalls.Count;
            if (result is not null) e.Fix = result;
            if (reply.Usage is { } used) e.LouisAgentReplies.Add(used);
        }
        if (reply.Currency is { } currency) _currency = currency;

        var md = new StringBuilder();
        md.AppendLine($"### louis-agent → orchestrator · reply {_current?.Messages ?? 0} · {reply.ToolCalls.Count} tool call(s) · stop: {reply.StopReason ?? "none"}");
        md.AppendLine();
        if (reply.Usage is { } usage) md.AppendLine($"Usage: {UsageText.Describe(usage, _currency, display)}").AppendLine();
        if (reply.ToolCalls.Count > 0)
        {
            md.AppendLine("| # | Tool | Input | Result |");
            md.AppendLine("|---|------|-------|--------|");
            int i = 0;
            foreach (var call in reply.ToolCalls)
                md.AppendLine($"| {++i} | {call.Name}{(call.IsError ? " ⚠" : "")} | {Cell(call.Input)} | {Cell(call.Result ?? "(no result)")} |");
            md.AppendLine();
        }

        if (reply.Error is not null) md.AppendLine($"**Error:** {reply.Error}").AppendLine();
        md.AppendLine("Reply text:").AppendLine();
        md.AppendLine(Quote(string.IsNullOrWhiteSpace(reply.Text) ? "(no text)" : reply.Text.Trim()));
        md.AppendLine();
        md.AppendLine(result is null
            ? "FIX-RESULT: **none in this reply**"
            : $"FIX-RESULT: status **{result.Status}**{Field("commit", result.Commit)}{Field("branch", result.Branch)}{Field("tests", result.Tests)}{Field("reason", result.Reason)}");
        md.AppendLine();
        Append(md.ToString());
    }

    /// <summary>Something the orchestrator did or checked itself (verify, replay, tests, escalate, acknowledge).</summary>
    public void Check(string tool, string headline, string? detail = null)
    {
        _current?.Checks.Add($"{tool}: {headline}");
        var md = new StringBuilder();
        if (!_checksHeaderWritten)
        {
            md.AppendLine("### Orchestrator actions and checks").AppendLine();
            _checksHeaderWritten = true;
        }
        md.AppendLine($"- **{tool}** → {headline}");
        if (!string.IsNullOrWhiteSpace(detail))
        {
            md.AppendLine("  ```text");
            foreach (string line in detail.Trim().ReplaceLineEndings("\n").Split('\n')) md.AppendLine("  " + line);
            md.AppendLine("  ```");
        }
        Append(md.ToString());
    }

    /// <param name="triage">The triage's own usage (its <see cref="UsageScope"/>'s records); null when it isn't recorded.</param>
    /// <returns>What the event used, for <c>decisions.jsonl</c>.</returns>
    public EventUsage EndEvent(string outcome, string reason, string orchestratorAnswer, IReadOnlyList<UsageRecord>? triage = null)
    {
        if (_current is not { } e) return new EventUsage(null, null, null);
        e.Outcome = outcome;
        e.Reason = reason;

        if (triage is not null && triage.Any(r => r.Cost is not null)) _currency = UsageText.CurrencyOf(triage);
        // A triage whose requests all failed (e.g. timed out) measured nothing: "-", not "0 · price unknown".
        UsageTotal? triageTotal = triage is null || triage.Count == 0 ? null : UsageReport.Total("triage", triage);
        UsageTotal? fixTotal = e.LouisAgentReplies.Count == 0 ? null : UsageReport.Sum("louis-agent", e.LouisAgentReplies);
        UsageTotal?[] parts = [triageTotal, fixTotal];
        e.Usage = new EventUsage(triageTotal, fixTotal,
            parts.Any(p => p is not null) ? UsageReport.Sum("total", parts.OfType<UsageTotal>()) : null);

        var md = new StringBuilder();
        md.AppendLine().AppendLine("### Summary").AppendLine();
        md.AppendLine("| | |").AppendLine("|---|---|");
        md.AppendLine($"| Decision | **{outcome}** — {Cell(reason, 400)} |");
        if (e.Messages > 0)
        {
            md.AppendLine($"| louis-agent said | {(e.Fix is null ? "no FIX-RESULT" : $"{e.Fix.Status}{(e.Fix.Tests is null ? "" : $", tests {e.Fix.Tests}")}")} · {e.Messages} message(s) · {e.ToolCalls} tool call(s) |");
            md.AppendLine($"| Branch | {Code(e.Fix?.Branch)} |");
            md.AppendLine($"| Commit | {Code(e.Fix?.Commit)} |");
        }
        md.AppendLine($"| Orchestrator checked | {(e.Checks.Count == 0 ? "-" : Cell(string.Join(" · ", e.Checks), 600))} |");
        if (e.Usage.Total is { } total)
        {
            md.AppendLine($"| Usage: triage | {Describe(e.Usage.Triage)} |");
            md.AppendLine($"| Usage: louis-agent | {Describe(e.Usage.LouisAgent)} |");
            md.AppendLine($"| Usage: total | **{UsageText.Describe(total, _currency, display)}** |");
        }
        md.AppendLine().AppendLine("Orchestrator's closing answer:").AppendLine();
        md.AppendLine(Quote(orchestratorAnswer.Trim()));
        md.AppendLine();
        Append(md.ToString());
        _current = null;
        return e.Usage;
    }

    public void EndRun()
    {
        if (_events.Count == 0) return;
        var md = new StringBuilder();
        md.AppendLine().AppendLine("---").AppendLine().AppendLine("## Run summary").AppendLine();
        md.AppendLine($"Finished {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}. " +
                      string.Join(", ", _events.GroupBy(e => e.Outcome).OrderBy(g => g.Key).Select(g => $"{g.Count()} {g.Key}")) + ".");
        md.AppendLine();
        md.AppendLine("| # | Request | Exception | Decision | louis-agent | Branch | Commit | Tokens | Cost |");
        md.AppendLine("|---|---------|-----------|----------|-------------|--------|--------|--------|------|");
        foreach (var e in _events)
        {
            string agent = e.Messages == 0 ? "-" : $"{e.Fix?.Status ?? "no result"} ({e.Messages} msg, {e.ToolCalls} tools)";
            md.AppendLine($"| {e.Number} | `{e.Error.Method} {e.Error.Arguments}` | {ShortType(e.Error.ExceptionType)} | **{e.Outcome}** | {agent} | {Code(e.Fix?.Branch)} | {Code(e.Fix?.Commit)} | {Tokens(e.Usage.Total)} | {Cost(e.Usage.Total)} |");
        }

        // The run's total, split by agent: the figure to compare with the usage ledger's for this run id.
        var totals = _events.Select(e => e.Usage).ToList();
        if (totals.Any(u => u.Total is not null))
        {
            // A part no event measured (no fix asked for, say) is "-", not the "price unknown" of an empty sum.
            UsageTotal? Sum(string key, Func<EventUsage, UsageTotal?> part) =>
                totals.Select(part).OfType<UsageTotal>().ToList() is { Count: > 0 } parts ? UsageReport.Sum(key, parts) : null;
            UsageTotal runTotal = Sum("total", u => u.Total)!;
            md.AppendLine().AppendLine($"**Run total:** {UsageText.Describe(runTotal, _currency, display)} " +
                                       $"(triage {Cost(Sum("triage", u => u.Triage))}, louis-agent {Cost(Sum("louis-agent", u => u.LouisAgent))}).");
            // F14: a converted total says the rate it used and what was charged, so it's never taken for the bill.
            if (display is { Converts: true } && runTotal.Cost is { } charged)
                md.AppendLine().AppendLine($"Shown in {display.Code} at {UsageText.Rate(display)} on {display.RateAsOf}; " +
                                           $"charged {UsageText.Money(charged, _currency)} (the ledger, fixes.jsonl and decisions.jsonl keep {_currency}).");
        }

        var branches = _events.Where(e => e.Outcome == "fixed" && e.Fix?.Branch is not null).Select(e => e.Fix!.Branch!).ToList();
        if (branches.Count > 0)
        {
            md.AppendLine().AppendLine($"Branches ready to review and merge ({branches.Count}):").AppendLine();
            foreach (string branch in branches) md.AppendLine($"- `{branch}`");
        }
        md.AppendLine();
        Append(md.ToString());
        _events.Clear();
    }

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    private void Append(string markdown)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.AppendAllText(Path, markdown.ReplaceLineEndings("\n"));
    }

    private static string ShortType(string type) => type[(type.LastIndexOf('.') + 1)..];

    private static string Field(string name, string? value) => value is null ? "" : $" · {name} `{value}`";

    private static string Code(string? value) => value is null ? "-" : $"`{value}`";

    private string Describe(UsageTotal? usage) => usage is null ? "-" : UsageText.Describe(usage, _currency, display);

    private static string Tokens(UsageTotal? usage) =>
        usage is null ? "-" : UsageText.Tokens(usage.Input + usage.CacheWrite + usage.CacheRead + usage.Output);

    private string Cost(UsageTotal? usage) => usage is null ? "-" : UsageText.Cost(usage, _currency, display);

    /// <summary>
    /// A usage total for the jsonl files, with louis-agent.api's field names so every file reads alike; null when not
    /// measured.
    /// </summary>
    internal static object? Json(UsageTotal? usage, string? currency) => usage is null ? null : new
    {
        requests = usage.Requests,
        input_tokens = usage.Input,
        cache_creation_input_tokens = usage.CacheWrite,
        cache_read_input_tokens = usage.CacheRead,
        output_tokens = usage.Output,
        cost = usage.Cost,
        currency = usage.Cost is null ? null : currency,
        unpriced = usage.Unpriced,
    };

    /// <summary>The run's currency so far, for the jsonl files.</summary>
    internal string Currency => _currency;

    private static string Quote(string text) =>
        string.Join("\n", text.ReplaceLineEndings("\n").Split('\n').Select(line => line.Length == 0 ? ">" : "> " + line));

    /// <summary>A value that fits in one table cell: one line, pipes escaped, shortened.</summary>
    internal static string Cell(string value, int max = MaxCellChars)
    {
        string flat = value.ReplaceLineEndings(" ⏎ ").Replace("|", "\\|").Replace("`", "'").Trim();
        return flat.Length <= max ? flat : flat[..max] + "…";
    }
}
