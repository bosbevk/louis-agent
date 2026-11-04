using System.Diagnostics;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;
using LouisAgent.Orchestrator.Comms;
using LouisAgent.Orchestrator.Tools;
using LouisAgent.Orchestrator.Triage;
using LouisAgent.Testing;

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
        foreach (var file in new DirectoryInfo(_dir).EnumerateFiles("*", SearchOption.AllDirectories))
            file.Attributes = FileAttributes.Normal;
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
        Assert.That(new ErrorFeed(log, Path.Combine(_dir, "state")).ReadNew().Select(e => e.Id),
            Is.EqualTo(new[] { "bbb222" }),
            "processed ids survive a restart");
    }

    [Test]
    public void ErrorFeed_IgnoresIdsThatAreNotPlainTokens()
    {
        string log = Path.Combine(_dir, "errors.jsonl");
        File.WriteAllLines(log, [Event("x; rm -rf /"), Event("ok_1")]);

        Assert.That(new ErrorFeed(log, _dir).ReadNew().Select(e => e.Id), Is.EqualTo(new[] { "ok_1" }));
    }

    [Test]
    public void TheWorkflow_IsBuiltIn_OnlyTheRunbooksAreOnDisk()
    {
        // CL9 step 7: the orchestrator's prompt is a mode in core (louis-agent/voice-orchestrator), read from the
        // assembly; its own Skills/ keeps the per-service runbooks.
        string skills = Directory.CreateTempSubdirectory("orchestrator-skills-").FullName;
        try
        {
            string service = Path.Combine(skills, "order-service");
            Directory.CreateDirectory(service);
            foreach (string file in Directory.EnumerateFiles(
                         Path.Combine(AppContext.BaseDirectory, "Skills", "order-service"), "*.md"))
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
        Assert.Throws<InvalidOperationException>(() =>
            OrchestratorSkills.Load(Path.Combine(AppContext.BaseDirectory, "Skills"), "nope"));

    [Test]
    public void FixPrompt_FencesTheEventAndNamesTheBranch()
    {
        var feed = FeedWith(Event("abc123"));
        var tools = Tools(feed);
        string prompt = tools.FixPrompt(feed.Find("abc123")!, "unknown codes give no discount",
            "fix/order-total-abc123");

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
        Assert.That(File.ReadAllText(Path.Combine(_dir, "state", "escalations.jsonl")),
            Does.Contain("card ref missing"));
    }

    [Test]
    public void Stopping_OnlyTheUsersStopEndsTheRun()
    {
        // FX7: a model request that times out throws TaskCanceledException, an OperationCanceledException; it must
        // count as that error's failure, not end the run as Ctrl+C does.
        var timeout = new TaskCanceledException(
            "The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.",
            new TimeoutException());
        using var stop = new CancellationTokenSource();

        Assert.That(Stopping.ByUser(timeout, stop.Token), Is.False, "a timeout while the run goes on");
        Assert.That(Stopping.ByUser(new InvalidOperationException(), stop.Token), Is.False);

        stop.Cancel();
        Assert.That(Stopping.ByUser(new OperationCanceledException(stop.Token), stop.Token), Is.True, "Ctrl+C");
        Assert.That(Stopping.ByUser(new InvalidOperationException(), stop.Token), Is.False,
            "another error while stopping");
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
}
