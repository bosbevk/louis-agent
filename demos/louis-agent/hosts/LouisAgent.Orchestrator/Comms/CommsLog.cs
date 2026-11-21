using LouisAgent.Composition.Tools;
using System.Text;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;
using LouisAgent.Orchestrator.Client;
using LouisAgent.Packs.Triage.Feed;
using static LouisAgent.Orchestrator.Comms.CommsMarkdown;

namespace LouisAgent.Orchestrator.Comms;

/// <summary>
/// One Markdown file with everything the agents said to each other: per error event, each message the orchestrator
/// sent louis-agent (verbatim), each reply (its text verbatim, plus every tool call it made), the orchestrator's own
/// checks, and a summary; then a run summary table for testing. Written as it happens, so a half-finished run is
/// readable.
/// </summary>
/// <remarks>
/// This class keeps the flow (a run, its errors, their messages) and the file; the Markdown pieces are
/// <see cref="CommsMarkdown"/>'s and the end of a run is <see cref="RunSummary"/>'s, split by what each would change
/// for (ADR 0034).
/// </remarks>
/// <param name="path">Where the file goes.</param>
/// <param name="display">The currency costs are shown in (F14); null shows them as charged. jsonl files keep what was
/// charged.</param>
/// <param name="time">The clock for the run's start and end; the system's unless a test gives its own.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0034-code-a-person-can-maintain.md">ADR 0034: code a person can maintain</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
internal sealed class CommsLog(string path, DisplayCurrency? display = null, TimeProvider? time = null)
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    private readonly List<EventSummary> _events = [];
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private EventSummary? _current;
    private bool _checksHeaderWritten;

    /// <summary>Where the file is, in full.</summary>
    public string Path { get; } = System.IO.Path.GetFullPath(path);

    /// <summary>
    /// The run's currency so far, for the jsonl files. One per run: both agents price from the same table (the demo's
    /// orchestrator reads louis-agent's /v1/prices).
    /// </summary>
    internal string Currency { get; private set; } = "USD";

    /// <summary>Starts a run's section: the service, louis-agent, the model, and how to read what follows.</summary>
    public void StartRun(string service, Uri louisAgentUrl, string model)
    {
        _events.Clear();
        Append($"""
                # Agent communications: {service}

                Run started {_time.GetUtcNow():yyyy-MM-dd HH:mm:ss} UTC · louis-agent at {louisAgentUrl} · model `{model}`

                Each error below shows what the orchestrator sent louis-agent and what louis-agent replied (text verbatim;
                tool inputs and results shortened to {MaxCellChars} characters), the orchestrator's own checks, and a summary.
                The run summary is at the end.

                """);
    }

    /// <summary>Starts one error's section, with the error event as the service logged it.</summary>
    public void StartEvent(int number, int total, ServiceError error)
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
        _current?.Took(reply, result);
        if (reply.Currency is { } currency) Currency = currency;

        var md = new StringBuilder();
        md.AppendLine($"### louis-agent → orchestrator · reply {_current?.Messages ?? 0} · " +
                      $"{reply.ToolCalls.Count} tool call(s) · stop: {reply.StopReason ?? "none"}");
        md.AppendLine();
        if (reply.Usage is { } usage)
            md.AppendLine($"Usage: {UsageText.Describe(usage, Currency, display)}").AppendLine();
        if (reply.ToolCalls.Count > 0)
        {
            md.AppendLine("| # | Tool | Input | Result |");
            md.AppendLine("|---|------|-------|--------|");
            int i = 0;
            foreach (var call in reply.ToolCalls)
                md.AppendLine($"| {++i} | {call.Name}{(call.IsError ? " ⚠" : "")} | {Cell(call.Input)} | " +
                              $"{Cell(call.Result ?? "(no result)")} |");
            md.AppendLine();
        }

        if (reply.Error is not null) md.AppendLine($"**Error:** {reply.Error}").AppendLine();
        md.AppendLine("Reply text:").AppendLine();
        md.AppendLine(Quote(string.IsNullOrWhiteSpace(reply.Text) ? "(no text)" : reply.Text.Trim()));
        md.AppendLine();
        md.AppendLine(FixResultLine(result));
        md.AppendLine();
        Append(md.ToString());
    }

    /// <summary>Something the orchestrator did or checked itself: a verify, replay, test run or hand-off.</summary>
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

    /// <summary>Ends an error's section with the decision, what was checked and what it all used.</summary>
    /// <param name="triage">The triage's own usage (its <see cref="UsageScope"/>'s records); null when it isn't
    /// recorded.</param>
    /// <returns>What the event used, for <c>decisions.jsonl</c>.</returns>
    public EventUsage EndEvent(TriageOutcome outcome, string reason, string orchestratorAnswer,
        IReadOnlyList<UsageRecord>? triage = null)
    {
        if (_current is not { } e) return EventUsage.None;
        e.Outcome = outcome;
        e.Reason = reason;
        if (triage is not null && triage.Any(r => r.Cost is not null)) Currency = UsageText.CurrencyOf(triage);
        e.Usage = EventUsage.Of(triage, e.LouisAgentReplies);

        var md = new StringBuilder();
        md.AppendLine().AppendLine("### Summary").AppendLine();
        md.AppendLine("| | |").AppendLine("|---|---|");
        md.AppendLine($"| Decision | **{outcome.Wire()}** — {Cell(reason, 400)} |");
        if (e.Messages > 0)
        {
            md.AppendLine($"| louis-agent said | {FixSaid(e.Fix)} · {e.Messages} message(s) · " +
                          $"{e.ToolCalls} tool call(s) |");
            md.AppendLine($"| Branch | {Code(e.Fix?.Branch)} |");
            md.AppendLine($"| Commit | {Code(e.Fix?.Commit)} |");
        }

        md.AppendLine(
            $"| Orchestrator checked | {(e.Checks.Count == 0 ? "-" : Cell(string.Join(" · ", e.Checks), 600))} |");
        if (e.Usage.Total is { } total)
        {
            md.AppendLine($"| Usage: triage | {Usage(e.Usage.Triage, Currency, display)} |");
            md.AppendLine($"| Usage: louis-agent | {Usage(e.Usage.LouisAgent, Currency, display)} |");
            md.AppendLine($"| Usage: total | **{UsageText.Describe(total, Currency, display)}** |");
        }

        md.AppendLine().AppendLine("Orchestrator's closing answer:").AppendLine();
        md.AppendLine(Quote(orchestratorAnswer.Trim()));
        md.AppendLine();
        Append(md.ToString());
        _current = null;
        return e.Usage;
    }

    /// <summary>Ends the run with its summary table, if it handled any errors.</summary>
    public void EndRun()
    {
        if (_events.Count == 0) return;
        Append(new RunSummary(_events, Currency, display).Write(_time.GetUtcNow()));
        _events.Clear();
    }

    /// <summary>Adds Markdown to the file, with Unix line endings whatever the machine.</summary>
    private void Append(string markdown)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.AppendAllText(Path, markdown.ReplaceLineEndings("\n"));
    }
}
