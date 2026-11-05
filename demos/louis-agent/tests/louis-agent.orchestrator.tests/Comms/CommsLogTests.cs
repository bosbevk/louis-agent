using LouisAgent.Composition.Tools;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;
using LouisAgent.Orchestrator.Comms;

namespace LouisAgent.Orchestrator.Tests.Comms;

/// <summary>
/// The comms log: everything the two agents said to each other, the orchestrator's own checks, and what each error and
/// the run used, as a person reads it afterwards.
/// </summary>
public class CommsLogTests
{
    private string _dir = null!;

    /// <summary>Each test writes its log to a folder of its own.</summary>
    [SetUp]
    public void SetUp() => _dir = Directory.CreateTempSubdirectory("comms-log-").FullName;

    /// <summary>Clears the test's folder.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    /// <summary>A run's start and end are stamped in UTC, whatever the machine's time zone.</summary>
    [Test]
    public void StartAndEnd_AreStampedInUtc()
    {
        string path = Path.Combine(_dir, "comms.md");
        var at = new DateTimeOffset(2026, 11, 4, 20, 30, 0, TimeSpan.FromHours(2));
        var comms = new CommsLog(path, time: new FixedClock(at));

        comms.StartRun("order-service", new Uri("http://127.0.0.1:5081"), "claude-test");
        comms.StartEvent(1, 1, Error("abc123"));
        comms.EndEvent(TriageOutcome.Ignored, "expected", "DECISION: ignored - expected");
        comms.EndRun();

        string md = File.ReadAllText(path);
        Assert.That(md, Does.Contain("Run started 2026-11-04 18:30:00 UTC"));
        Assert.That(md, Does.Contain("Finished 2026-11-04 18:30:00 UTC. 1 ignored."));
    }

    /// <summary>Messages, replies (tool calls, FIX-RESULT) and checks land in the log, then a run summary.</summary>
    [Test]
    public void RecordsMessagesRepliesChecksAndARunSummary()
    {
        string path = Path.Combine(_dir, "comms.md");
        var comms = new CommsLog(path);
        var call = new AgentToolCall("t1", "CreateBranch", "{\"branchName\":\"fix/order-total-abc123\"}")
        { Result = "Success | done" };
        var reply = new AgentReply(
            "Fixed.\nFIX-RESULT: {\"status\":\"fixed\",\"commit\":\"abc1234\",\"branch\":\"fix/order-total-abc123\",\"tests\":\"5/5 passed\"}",
            [call], "end_turn", null);

        comms.StartRun("order-service", new Uri("http://127.0.0.1:5081"), "claude-test");
        comms.StartEvent(1, 1, Error("abc123"));
        comms.Sent("sess_1", "Please fix ```json {} ``` this");
        comms.Received(reply, LouisAgentClient.ParseFixResult(reply.Text));
        comms.Check("ReplayRequest(order-total 1003)", "`200 order-total 1003: 35.00`");
        comms.EndEvent(TriageOutcome.Fixed, "verified", "All checks passed.\nDECISION: fixed - verified");
        comms.EndRun();

        string md = File.ReadAllText(path);
        Assert.That(md, Does.Contain("# Agent communications: order-service"));
        Assert.That(md, Does.Contain("### Orchestrator → louis-agent · message 1 · session `sess_1`"));
        Assert.That(md, Does.Contain("````text\nPlease fix ```json {} ``` this\n````"),
            "a message with its own fences stays intact");
        Assert.That(md, Does.Contain("| 1 | CreateBranch |"));
        Assert.That(md, Does.Contain("Success \\| done"), "pipes in a result don't break the table");
        Assert.That(md, Does.Contain("> Fixed."));
        Assert.That(md, Does.Contain("FIX-RESULT: status **fixed** · commit `abc1234`"));
        Assert.That(md, Does.Contain("- **ReplayRequest(order-total 1003)** → `200 order-total 1003: 35.00`"));
        Assert.That(md, Does.Contain("## Run summary"));
        Assert.That(md,
            Does.Contain(
                "| 1 | `order-total 1003` | KeyNotFoundException | **fixed** | fixed (1 msg, 1 tools) | `fix/order-total-abc123` | `abc1234` |"));
        Assert.That(md, Does.Contain("Branches ready to review and merge (1)"));
        Assert.That(md, Does.Not.Contain("Run total"), "nothing measured: no usage in the log");
    }

    /// <summary>What each reply, each error and the whole run used, split by agent.</summary>
    [Test]
    public void ShowsUsagePerReplyPerErrorAndForTheRun()
    {
        // F3 step 7: louis-agent's usage per reply, triage + louis-agent per error, and the run's total, which is the
        // figure to compare with the usage ledger's total for the run.
        string path = Path.Combine(_dir, "comms.md");
        var comms = new CommsLog(path);

        static AgentReply Reply(int requests, long input, long cacheRead, long output, decimal cost) =>
            new("Working.", [], "end_turn", null,
                new UsageTotal("louis-agent", requests, input, 0, cacheRead, output, cost, 0), "USD");

        static UsageRecord Triage(long input, long output, decimal cost) => new(
            DateTimeOffset.UtcNow, 1, UsagePurpose.Triage, "orchestrator", "triage_x", 1, null, "run-1",
            "order-service",
            "claude-haiku-5-5", new UsageTokens(input, 0, 0, output, null), 1, "stop",
            new UsageCost("USD", cost, "2026-10-10"));

        comms.StartRun("order-service", new Uri("http://127.0.0.1:5081"), "claude-test");
        comms.StartEvent(1, 2, Error("abc123"));
        comms.Sent("sess_1", "Please fix it");
        comms.Received(Reply(3, 1_000, 20_000, 300, 0.01m), null);
        comms.Sent("sess_1", "Continue");
        comms.Received(Reply(2, 500, 10_000, 200, 0.005m), null);
        EventUsage first = comms.EndEvent(TriageOutcome.Fixed, "verified", "DECISION: fixed - verified",
            [Triage(2_000, 100, 0.002m), Triage(2_000, 100, 0.002m)]);
        comms.StartEvent(2, 2, Error("def456"));
        EventUsage second = comms.EndEvent(TriageOutcome.Ignored, "expected", "DECISION: ignored - expected",
            [Triage(3_000, 50, 0.001m)]);
        comms.EndRun();

        string md = File.ReadAllText(path);
        Assert.That(md, Does.Contain("Usage: 3 requests · 21k tokens (20k cached) · $0.01"), "each reply");
        Assert.That(md, Does.Contain("| Usage: triage | 2 requests · 4.2k tokens · $0.004 |"));
        Assert.That(md, Does.Contain("| Usage: louis-agent | 5 requests · 32k tokens (30k cached) · $0.015 |"));
        Assert.That(md, Does.Contain("| Usage: total | **7 requests · 36k tokens (30k cached) · $0.019** |"));
        Assert.That(md, Does.Contain("| Usage: louis-agent | - |"), "no fix for the second error");
        Assert.That(md, Does.Contain("| Branch | Commit | Tokens | Cost |"));
        Assert.That(md, Does.Contain("| 36k | $0.019 |"));
        Assert.That(md, Does.Contain("| 3.1k | $0.001 |"));
        Assert.That(md,
            Does.Contain(
                "**Run total:** 8 requests · 39k tokens (30k cached) · $0.02 (triage $0.005, louis-agent $0.015)."));

        Assert.That((first.Total!.Requests, first.Total.Cost, first.LouisAgent!.Cost, first.Triage!.Cost),
            Is.EqualTo((7, 0.019m, 0.015m, 0.004m)));
        Assert.That((second.LouisAgent, second.Total!.Cost), Is.EqualTo(((UsageTotal?)null, 0.001m)));
    }

    /// <summary>Shown in rands, the log says the rate it used and what was charged in dollars.</summary>
    [Test]
    public void InRands_ShowsTheRateAndWhatWasCharged()
    {
        // F14: the comms log in the display currency, with the rate and the charged total; the jsonl figures stay in
        // USD.
        var rands = new DisplayCurrency("ZAR", "R", 16.5331m, "2026-10-09", "USD", "HOST_CULTURE en-ZA");
        string path = Path.Combine(_dir, "comms.md");
        var comms = new CommsLog(path, rands);
        var reply = new AgentReply("Working.", [], "end_turn", null,
            new UsageTotal("louis-agent", 3, 1_000, 0, 20_000, 300, 0.01m, 0), "USD");
        var triage = new UsageRecord(DateTimeOffset.UtcNow, 1, UsagePurpose.Triage, "orchestrator", "triage_x", 1, null,
            "run-1",
            "order-service", "claude-haiku-5-5", new UsageTokens(2_000, 0, 0, 100, null), 1, "stop",
            new UsageCost("USD", 0.002m, "2026-10-10"));

        comms.StartRun("order-service", new Uri("http://127.0.0.1:5081"), "claude-test");
        comms.StartEvent(1, 1, Error("abc123"));
        comms.Sent("sess_1", "Please fix it");
        comms.Received(reply, null);
        EventUsage used = comms.EndEvent(TriageOutcome.Fixed, "verified", "DECISION: fixed - verified", [triage]);
        comms.EndRun();

        string md = File.ReadAllText(path);
        Assert.That(md, Does.Contain("Usage: 3 requests · 21k tokens (20k cached) · R0.17"), "each reply");
        Assert.That(md, Does.Contain("| Usage: total | **4 requests · 23k tokens (20k cached) · R0.2** |"));
        Assert.That(md, Does.Contain("| 23k | R0.2 |"));
        Assert.That(md,
            Does.Contain(
                "**Run total:** 4 requests · 23k tokens (20k cached) · R0.2 (triage R0.033, louis-agent R0.17)."));
        Assert.That(md,
            Does.Contain(
                "Shown in ZAR at 16.5331 ZAR/USD on 2026-10-09; charged $0.012 (the ledger, fixes.jsonl and decisions.jsonl keep USD)."));
        Assert.That((used.Total!.Cost, UsageLine.Of(used.Total, comms.Currency)),
            Is.EqualTo(((decimal?)0.012m, UsageLine.Of(used.Total, "USD"))),
            "decisions.jsonl gets the charged USD figures");
    }

    /// <summary>A figure nothing measured shows as a dash, never as "price unknown".</summary>
    [Test]
    public void NothingMeasured_ShowsDashesNotPriceUnknown()
    {
        // Seen in the split demo: a triage that timed out completed no request, and no fix was asked for.
        string path = Path.Combine(_dir, "comms.md");
        var comms = new CommsLog(path);
        var local = new UsageRecord(DateTimeOffset.UtcNow, 1, UsagePurpose.Triage, "orchestrator", "triage_x", 1, null,
            "run-1",
            "order-service", "llama3.1", new UsageTokens(2_050, 0, 0, 3, null), 1, "stop",
            new UsageCost("USD", 0m, "2026-10-10"));

        comms.StartRun("order-service", new Uri("http://127.0.0.1:5081"), "llama3.1");
        comms.StartEvent(1, 2, Error("abc123"));
        EventUsage timedOut = comms.EndEvent(TriageOutcome.Failed, "triage failed: timeout", "", []);
        comms.StartEvent(2, 2, Error("def456"));
        comms.EndEvent(TriageOutcome.Ignored, "expected", "DECISION: ignored - expected", [local]);
        comms.EndRun();

        string md = File.ReadAllText(path);
        Assert.That(timedOut.Total, Is.Null);
        Assert.That(md, Does.Contain("| **failed** | - | - | - | - | - |"), "row 1: nothing measured");
        Assert.That(md, Does.Contain("| 2.1k | $0 |"), "row 2: a local model is free");
        Assert.That(md, Does.Contain("**Run total:** 1 request · 2.1k tokens · $0 (triage $0, louis-agent -)."));
    }

    /// <summary>An error event as the service logs it.</summary>
    private static ErrorEvent Error(string id) =>
        new(id, DateTimeOffset.UnixEpoch, "order-service", "order-total", "1003",
            "System.Collections.Generic.KeyNotFoundException", "The given key 'SUMMER25' was not present",
            "   at OrderService.OrderApi.GetOrderTotal(Int32 id)");

    /// <summary>A clock that always says the same time.</summary>
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        /// <summary>The time it was made with, in UTC.</summary>
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
    }
}
