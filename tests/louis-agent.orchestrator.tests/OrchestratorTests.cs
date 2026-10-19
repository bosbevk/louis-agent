using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;
using System.Diagnostics;
using LouisAgent.Orchestrator.Tools;

namespace LouisAgent.Orchestrator.Tests;

public class OrchestratorTests
{
    private string _dir = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "orchestrator-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_dir);
    }

    [TearDown]
    public void TearDown()
    {
        // git marks objects read-only on Windows
        foreach (var file in new DirectoryInfo(_dir).EnumerateFiles("*", SearchOption.AllDirectories)) file.Attributes = FileAttributes.Normal;
        Directory.Delete(_dir, recursive: true);
    }

    private static string Event(string id, string method = "order-total") =>
        $$"""{"id":"{{id}}","timestamp":"2026-10-06T10:00:00+00:00","service":"order-service","method":"{{method}}","arguments":"1003","exception_type":"System.Collections.Generic.KeyNotFoundException","message":"The given key 'SUMMER25' was not present","stack_trace":"   at OrderService.OrderApi.GetOrderTotal(Int32 id)"}""";

    [Test]
    public void ErrorFeed_ReturnsEachEventUntilItIsProcessed()
    {
        string log = Path.Combine(_dir, "errors.jsonl");
        File.WriteAllLines(log, [Event("aaa111"), "not json", Event("bbb222")]);
        var feed = new ErrorFeed(log, Path.Combine(_dir, "state"));

        Assert.That(feed.ReadNew().Select(e => e.Id), Is.EqualTo(new[] { "aaa111", "bbb222" }));
        Assert.That(feed.Find("aaa111")!.Method, Is.EqualTo("order-total"));

        feed.MarkProcessed("aaa111");
        Assert.That(new ErrorFeed(log, Path.Combine(_dir, "state")).ReadNew().Select(e => e.Id), Is.EqualTo(new[] { "bbb222" }),
            "processed ids survive a restart");
    }

    [Test]
    public void ErrorFeed_IgnoresIdsThatAreNotPlainTokens()
    {
        string log = Path.Combine(_dir, "errors.jsonl");
        File.WriteAllLines(log, [Event("x; rm -rf /"), Event("ok_1")]);

        Assert.That(new ErrorFeed(log, _dir).ReadNew().Select(e => e.Id), Is.EqualTo(new[] { "ok_1" }));
    }

    [TestCase("Done.\nDECISION: fixed - commit abc1234 verified", "fixed", "commit abc1234 verified")]
    [TestCase("**DECISION:** ignored — expected 404", "ignored", "expected 404")]
    [TestCase("DECISION: Escalated: payments must check the refund", "escalated", "payments must check the refund")]
    public void Decision_ParsesTheClosingLine(string answer, string outcome, string reason)
    {
        var decision = Decision.Parse(answer);

        Assert.That(decision.Outcome, Is.EqualTo(outcome));
        Assert.That(decision.Reason, Is.EqualTo(reason));
    }

    [Test]
    public void Decision_Record_UsesSnakeCaseKeys_LikeTheOtherRecords()
    {
        var error = FeedWith(Event("abc123")).Find("abc123")!;
        var used = new EventUsage(null, null, null);

        string json = System.Text.Json.JsonSerializer.Serialize(
            Decision.Parse("DECISION: fixed - verified").ToRecord(error, used, "USD", new DateTimeOffset(2026, 10, 12, 8, 0, 0, TimeSpan.Zero)));

        Assert.That(json, Does.StartWith("""{"error_id":"abc123","method":"order-total","outcome":"fixed","reason":"verified","at":"2026-10-12T08:00:00+00:00","""));
        Assert.That(json, Does.EndWith(""","usage":null,"triage_usage":null,"louis_agent_usage":null}"""));
    }

    [Test]
    public void Decision_WithoutALine_IsUnknown() =>
        Assert.That(Decision.Parse("I looked at it.").Outcome, Is.EqualTo("unknown"));

    [Test]
    public void TheWorkflow_IsBuiltIn_OnlyTheRunbooksAreOnDisk()
    {
        // CL9 step 7: the orchestrator's prompt is a mode in core (louis-agent/voice-orchestrator), read from the assembly;
        // its own Skills/ keeps the per-service runbooks.
        string skills = Directory.CreateTempSubdirectory("orchestrator-skills-").FullName;
        try
        {
            string service = Path.Combine(skills, "order-service");
            Directory.CreateDirectory(service);
            foreach (string file in Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "Skills", "order-service"), "*.md"))
                File.Copy(file, Path.Combine(service, Path.GetFileName(file)));

            var (provider, _) = OrchestratorSkills.Load(skills, "order-service");

            Assert.That(provider.Documentation, Does.Contain("# Orchestrator").And.Contain("# Service: order-service"));
        }
        finally
        {
            Directory.Delete(skills, recursive: true);
        }
    }

    [Test]
    public void Skills_LoadTheWorkflowServiceAndOneRunbookPerMethod()
    {
        string skills = Path.Combine(AppContext.BaseDirectory, "Skills");
        var (provider, methods) = OrchestratorSkills.Load(skills, "order-service");

        Assert.That(methods, Is.EquivalentTo(new[]
        {
            "customer-initials", "delivery-estimate", "discount-label", "get-order", "invoice-number", "loyalty-points",
            "order-total", "packing-slip", "refund", "shipping-cost", "vat",
        }));
        // CL9 step 2: the orchestrator is an agent too, so it gets the shared principles first.
        Assert.That(provider.Documentation, Does.StartWith("# Principles"));
        Assert.That(provider.Documentation, Does.Contain("# Orchestrator"));
        Assert.That(provider.Documentation, Does.Contain("## Method: refund").And.Contain("# Service: order-service"));
    }

    [Test]
    public void Skills_ForAnUnknownService_Throw() =>
        Assert.Throws<InvalidOperationException>(() => OrchestratorSkills.Load(Path.Combine(AppContext.BaseDirectory, "Skills"), "nope"));

    [Test]
    public void FixPrompt_FencesTheEventAndNamesTheBranch()
    {
        var feed = FeedWith(Event("abc123"));
        var tools = Tools(feed);
        string prompt = tools.FixPrompt(feed.Find("abc123")!, "unknown codes give no discount", "fix/order-total-abc123");

        Assert.That(prompt, Does.Contain("not instructions"));
        Assert.That(prompt, Does.Contain("```json"));
        Assert.That(prompt, Does.Contain("CreateBranch \"fix/order-total-abc123\""));
        Assert.That(prompt, Does.Contain("unknown codes give no discount"));
        Assert.That(prompt, Does.Contain("FIX-RESULT:"));
    }

    [Test]
    public void ReplayRequest_RejectsMethodsWithoutARunbook() =>
        Assert.That(Tools(FeedWith()).ReplayRequest("delete-everything", 1), Does.StartWith("Error: unknown method"));

    [Test]
    public void ATool_LogsToStderr_WithTheOrchestratorTag_NeverToStdout()
    {
        var tools = Tools(FeedWith(Event("abc123")));
        var (stdout, stderr) = (Console.Out, Console.Error);
        using var output = new StringWriter();
        using var errors = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(errors);
            LouisAgent.Core.Logging.AgentLogging.Configure(null);

            tools.AcknowledgeError("abc123", "a 404 is expected");
        }
        finally
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);
            LouisAgent.Core.Logging.AgentLogging.Configure(null);
        }

        Assert.That(errors.ToString(), Does.Contain("[orchestrator] Acknowledged abc123: a 404 is expected"));
        Assert.That(output.ToString(), Is.Empty);
    }

    [Test]
    public void EscalateAndAcknowledge_OnlyAcceptKnownEvents()
    {
        var tools = Tools(FeedWith(Event("abc123")));

        Assert.That(tools.EscalateToHuman("nope", "payments", "x"), Does.StartWith("Error"));
        Assert.That(tools.EscalateToHuman("abc123", "payments", "card ref missing"), Does.Contain("payments"));
        Assert.That(tools.AcknowledgeError("abc123", "404"), Does.Contain("no action"));
        Assert.That(File.ReadAllText(Path.Combine(_dir, "state", "escalations.jsonl")), Does.Contain("card ref missing"));
    }

    [Test]
    public void VerifyFixCommit_PassesForACommitOnTheFixBranch_AndFailsForOneOnMain()
    {
        if (!GitAvailable()) Assert.Ignore("git is not installed");
        InitRepo();
        string mainCommit = Git("rev-parse", "HEAD");
        Git("switch", "-c", "fix/order-total-abc123");
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "two");
        Git("commit", "-am", "fix");
        string fixCommit = Git("rev-parse", "HEAD");

        var tools = Tools(FeedWith());
        Assert.That(tools.VerifyFixCommit(fixCommit, "fix/order-total-abc123"), Does.EndWith("VERIFIED" + Environment.NewLine).And.Not.Contain("FAIL"));
        Assert.That(tools.VerifyFixCommit(mainCommit, "fix/order-total-abc123"), Does.Contain("FAIL commit is not on main").And.Contain("NOT VERIFIED"));
        Assert.That(tools.VerifyFixCommit("deadbeef", "fix/order-total-abc123"), Does.Contain("FAIL commit exists"));
        Assert.That(tools.VerifyFixCommit("--all", "fix/x"), Does.StartWith("Error"));
    }

    [Test]
    public void ReturnToMain_StashesLeftoversAndChecksOutMain()
    {
        if (!GitAvailable()) Assert.Ignore("git is not installed");
        InitRepo();
        Git("switch", "-c", "fix/half-done");
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "unfinished");

        Assert.That(Tools(FeedWith()).ReturnToMain(), Is.Null);
        Assert.That(Git("rev-parse", "--abbrev-ref", "HEAD"), Is.EqualTo("main"));
        Assert.That(Git("stash", "list"), Does.Contain("uncommitted changes left on fix/half-done"));
    }

    [Test]
    public void CommsLog_RecordsMessagesRepliesChecksAndARunSummary()
    {
        var feed = FeedWith(Event("abc123"));
        string path = Path.Combine(_dir, "comms.md");
        var comms = new CommsLog(path);
        var call = new AgentToolCall("t1", "CreateBranch", "{\"branchName\":\"fix/order-total-abc123\"}") { Result = "Success | done" };
        var reply = new AgentReply("Fixed.\nFIX-RESULT: {\"status\":\"fixed\",\"commit\":\"abc1234\",\"branch\":\"fix/order-total-abc123\",\"tests\":\"5/5 passed\"}",
            [call], "end_turn", null);

        comms.StartRun("order-service", new Uri("http://127.0.0.1:5081"), "claude-test");
        comms.StartEvent(1, 1, feed.Find("abc123")!);
        comms.Sent("sess_1", "Please fix ```json {} ``` this");
        comms.Received(reply, LouisAgentClient.ParseFixResult(reply.Text));
        comms.Check("ReplayRequest(order-total 1003)", "`200 order-total 1003: 35.00`");
        comms.EndEvent("fixed", "verified", "All checks passed.\nDECISION: fixed - verified");
        comms.EndRun();

        string md = File.ReadAllText(path);
        Assert.That(md, Does.Contain("# Agent communications: order-service"));
        Assert.That(md, Does.Contain("### Orchestrator → louis-agent · message 1 · session `sess_1`"));
        Assert.That(md, Does.Contain("````text\nPlease fix ```json {} ``` this\n````"), "a message with its own fences stays intact");
        Assert.That(md, Does.Contain("| 1 | CreateBranch |"));
        Assert.That(md, Does.Contain("Success \\| done"), "pipes in a result don't break the table");
        Assert.That(md, Does.Contain("> Fixed."));
        Assert.That(md, Does.Contain("FIX-RESULT: status **fixed** · commit `abc1234`"));
        Assert.That(md, Does.Contain("- **ReplayRequest(order-total 1003)** → `200 order-total 1003: 35.00`"));
        Assert.That(md, Does.Contain("## Run summary"));
        Assert.That(md, Does.Contain("| 1 | `order-total 1003` | KeyNotFoundException | **fixed** | fixed (1 msg, 1 tools) | `fix/order-total-abc123` | `abc1234` |"));
        Assert.That(md, Does.Contain("Branches ready to review and merge (1)"));
        Assert.That(md, Does.Not.Contain("Run total"), "nothing measured: no usage in the log");
    }

    [Test]
    public void CommsLog_ShowsUsagePerReplyPerErrorAndForTheRun()
    {
        // F3 step 7: louis-agent's usage per reply, triage + louis-agent per error, and the run's total, which is the
        // figure to compare with the usage ledger's total for the run.
        var feed = FeedWith(Event("abc123"), Event("def456"));
        string path = Path.Combine(_dir, "comms.md");
        var comms = new CommsLog(path);
        static AgentReply Reply(int requests, long input, long cacheRead, long output, decimal cost) =>
            new("Working.", [], "end_turn", null, new UsageTotal("louis-agent", requests, input, 0, cacheRead, output, cost, 0), "USD");
        static UsageRecord Triage(long input, long output, decimal cost) => new(
            DateTimeOffset.UtcNow, 1, UsagePurpose.Triage, "orchestrator", "triage_x", 1, null, "run-1", "order-service",
            "claude-haiku-5-5", new UsageTokens(input, 0, 0, output, null), 1, "stop", new UsageCost("USD", cost, "2026-10-10"));

        comms.StartRun("order-service", new Uri("http://127.0.0.1:5081"), "claude-test");
        comms.StartEvent(1, 2, feed.Find("abc123")!);
        comms.Sent("sess_1", "Please fix it");
        comms.Received(Reply(3, 1_000, 20_000, 300, 0.01m), null);
        comms.Sent("sess_1", "Continue");
        comms.Received(Reply(2, 500, 10_000, 200, 0.005m), null);
        EventUsage first = comms.EndEvent("fixed", "verified", "DECISION: fixed - verified", [Triage(2_000, 100, 0.002m), Triage(2_000, 100, 0.002m)]);
        comms.StartEvent(2, 2, feed.Find("def456")!);
        EventUsage second = comms.EndEvent("ignored", "expected", "DECISION: ignored - expected", [Triage(3_000, 50, 0.001m)]);
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
        Assert.That(md, Does.Contain("**Run total:** 8 requests · 39k tokens (30k cached) · $0.02 (triage $0.005, louis-agent $0.015)."));

        Assert.That((first.Total!.Requests, first.Total.Cost, first.LouisAgent!.Cost, first.Triage!.Cost), Is.EqualTo((7, 0.019m, 0.015m, 0.004m)));
        Assert.That((second.LouisAgent, second.Total!.Cost), Is.EqualTo(((UsageTotal?)null, 0.001m)));
    }

    [Test]
    public void CommsLog_InRands_ShowsTheRateAndWhatWasCharged()
    {
        // F14: the comms log in the display currency, with the rate and the charged total; the jsonl figures stay in USD.
        var rands = new DisplayCurrency("ZAR", "R", 16.5331m, "2026-10-09", "USD", "HOST_CULTURE en-ZA");
        var feed = FeedWith(Event("abc123"));
        string path = Path.Combine(_dir, "comms.md");
        var comms = new CommsLog(path, rands);
        var reply = new AgentReply("Working.", [], "end_turn", null, new UsageTotal("louis-agent", 3, 1_000, 0, 20_000, 300, 0.01m, 0), "USD");
        var triage = new UsageRecord(DateTimeOffset.UtcNow, 1, UsagePurpose.Triage, "orchestrator", "triage_x", 1, null, "run-1",
            "order-service", "claude-haiku-5-5", new UsageTokens(2_000, 0, 0, 100, null), 1, "stop", new UsageCost("USD", 0.002m, "2026-10-10"));

        comms.StartRun("order-service", new Uri("http://127.0.0.1:5081"), "claude-test");
        comms.StartEvent(1, 1, feed.Find("abc123")!);
        comms.Sent("sess_1", "Please fix it");
        comms.Received(reply, null);
        EventUsage used = comms.EndEvent("fixed", "verified", "DECISION: fixed - verified", [triage]);
        comms.EndRun();

        string md = File.ReadAllText(path);
        Assert.That(md, Does.Contain("Usage: 3 requests · 21k tokens (20k cached) · R0.17"), "each reply");
        Assert.That(md, Does.Contain("| Usage: total | **4 requests · 23k tokens (20k cached) · R0.2** |"));
        Assert.That(md, Does.Contain("| 23k | R0.2 |"));
        Assert.That(md, Does.Contain("**Run total:** 4 requests · 23k tokens (20k cached) · R0.2 (triage R0.033, louis-agent R0.17)."));
        Assert.That(md, Does.Contain("Shown in ZAR at 16.5331 ZAR/USD on 2026-10-09; charged $0.012 (the ledger, fixes.jsonl and decisions.jsonl keep USD)."));
        Assert.That((used.Total!.Cost, CommsLog.Json(used.Total, comms.Currency)!.ToString()), Is.EqualTo(((decimal?)0.012m, CommsLog.Json(used.Total, "USD")!.ToString())),
            "decisions.jsonl gets the charged USD figures");
    }

    [Test]
    public void CommsLog_NothingMeasured_ShowsDashesNotPriceUnknown()
    {
        // Seen in the split demo: a triage that timed out completed no request, and no fix was asked for.
        var feed = FeedWith(Event("abc123"), Event("def456"));
        string path = Path.Combine(_dir, "comms.md");
        var comms = new CommsLog(path);
        var local = new UsageRecord(DateTimeOffset.UtcNow, 1, UsagePurpose.Triage, "orchestrator", "triage_x", 1, null, "run-1",
            "order-service", "llama3.1", new UsageTokens(2_050, 0, 0, 3, null), 1, "stop", new UsageCost("USD", 0m, "2026-10-10"));

        comms.StartRun("order-service", new Uri("http://127.0.0.1:5081"), "llama3.1");
        comms.StartEvent(1, 2, feed.Find("abc123")!);
        EventUsage timedOut = comms.EndEvent("failed", "triage failed: timeout", "", []);
        comms.StartEvent(2, 2, feed.Find("def456")!);
        comms.EndEvent("acknowledged", "expected", "DECISION: acknowledged - expected", [local]);
        comms.EndRun();

        string md = File.ReadAllText(path);
        Assert.That(timedOut.Total, Is.Null);
        Assert.That(md, Does.Contain("| **failed** | - | - | - | - | - |"), "row 1: nothing measured");
        Assert.That(md, Does.Contain("| 2.1k | $0 |"), "row 2: a local model is free");
        Assert.That(md, Does.Contain("**Run total:** 1 request · 2.1k tokens · $0 (triage $0, louis-agent -)."));
    }

    [Test]
    public void Stopping_OnlyTheUsersStopEndsTheRun()
    {
        // FX7: a model request that times out throws TaskCanceledException, an OperationCanceledException; it must count
        // as that error's failure, not end the run as Ctrl+C does.
        var timeout = new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.",
            new TimeoutException());
        using var stop = new CancellationTokenSource();

        Assert.That(Stopping.ByUser(timeout, stop.Token), Is.False, "a timeout while the run goes on");
        Assert.That(Stopping.ByUser(new InvalidOperationException(), stop.Token), Is.False);

        stop.Cancel();
        Assert.That(Stopping.ByUser(new OperationCanceledException(stop.Token), stop.Token), Is.True, "Ctrl+C");
        Assert.That(Stopping.ByUser(new InvalidOperationException(), stop.Token), Is.False, "another error while stopping");
    }

    [Test]
    public void CommsLog_Json_UsesTheApisFieldNames()
    {
        var usage = new UsageTotal("total", 7, 5_500, 0, 30_000, 700, 0.019m, 0);

        string json = System.Text.Json.JsonSerializer.Serialize(CommsLog.Json(usage, "USD"));

        Assert.That(json, Is.EqualTo("{\"requests\":7,\"input_tokens\":5500,\"cache_creation_input_tokens\":0,\"cache_read_input_tokens\":30000," +
                                     "\"output_tokens\":700,\"cost\":0.019,\"currency\":\"USD\",\"unpriced\":0}"));
        Assert.That(CommsLog.Json(null, "USD"), Is.Null);
    }

    private void InitRepo()
    {
        Git("init", "-b", "main");
        Git("config", "user.email", "test@localhost");
        Git("config", "user.name", "test");
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "one");
        File.WriteAllText(Path.Combine(_dir, ".gitignore"), "errors.jsonl\nstate/\n"); // like the service's ignored logs/
        Git("add", ".");
        Git("commit", "-m", "initial");
    }

    private ErrorFeed FeedWith(params string[] events)
    {
        string log = Path.Combine(_dir, "errors.jsonl");
        File.WriteAllLines(log, events);
        var feed = new ErrorFeed(log, Path.Combine(_dir, "state"));
        feed.ReadNew();
        return feed;
    }

    private ServiceTools Tools(ErrorFeed feed)
    {
        var options = new OrchestratorOptions
        {
            ServiceRoot = _dir,
            StateDirectory = Path.Combine(_dir, "state"),
            SkillsDirectory = Path.Combine(AppContext.BaseDirectory, "Skills"),
        };
        var client = new LouisAgentClient(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1") }, null);
        return new ServiceTools(options, feed, client, new HashSet<string> { "get-order", "order-total", "refund" },
            new CommsLog(Path.Combine(_dir, "state", "agent-comms.md")));
    }

    private string Git(params string[] args)
    {
        var psi = new ProcessStartInfo("git") { WorkingDirectory = _dir, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string arg in args) psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi)!;
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output.Trim();
    }

    private static bool GitAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", "--version") { RedirectStandardOutput = true })!;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
