using System.Diagnostics;

namespace louis_agent.orchestrator.tests;

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
    public void Decision_WithoutALine_IsUnknown() =>
        Assert.That(Decision.Parse("I looked at it.").Outcome, Is.EqualTo("unknown"));

    [Test]
    public void Skills_LoadTheWorkflowServiceAndOneRunbookPerMethod()
    {
        string skills = Path.Combine(AppContext.BaseDirectory, "Skills");
        var (provider, methods) = OrchestratorSkills.Load(skills, "order-service");

        Assert.That(methods, Is.EquivalentTo(new[] { "get-order", "order-total", "refund" }));
        Assert.That(provider.Documentation, Does.StartWith("# Orchestrator"));
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
        Git("init", "-b", "main");
        Git("config", "user.email", "test@localhost");
        Git("config", "user.name", "test");
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "one");
        File.WriteAllText(Path.Combine(_dir, ".gitignore"), "errors.jsonl\nstate/\n"); // like the service's ignored logs/
        Git("add", ".");
        Git("commit", "-m", "initial");
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
        return new ServiceTools(options, feed, client, new HashSet<string> { "get-order", "order-total", "refund" }, _ => { });
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
