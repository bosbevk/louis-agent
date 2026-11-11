using System.Diagnostics;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;
using LouisAgent.Orchestrator.Client;
using LouisAgent.Orchestrator.Comms;
using LouisAgent.Orchestrator.Tools;
using LouisAgent.Orchestrator.Tools.Checks;
using LouisAgent.Packs.Triage.Feed;
using LouisAgent.Testing;

namespace LouisAgent.Orchestrator.IntegrationTests;

[Category("Integration")]
public class OrchestratorGitIntegrationTests
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

    [Test]
    public void VerifyFixCommit_PassesForACommitOnTheFixBranch_AndFailsForOneOnMain()
    {
        Outside.Tool(GitAvailable(), "git is not installed");
        InitRepo();
        string mainCommit = Git("rev-parse", "HEAD");
        Git("switch", "-c", "fix/order-total-abc123");
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "two");
        Git("commit", "-am", "fix");
        string fixCommit = Git("rev-parse", "HEAD");

        var checks = Checks();
        Assert.That(checks.VerifyFixCommit(fixCommit, "fix/order-total-abc123"),
            Does.EndWith("VERIFIED" + Environment.NewLine).And.Not.Contain("FAIL"));
        Assert.That(checks.VerifyFixCommit(mainCommit, "fix/order-total-abc123"),
            Does.Contain("FAIL commit is not on main").And.Contain("NOT VERIFIED"));
        Assert.That(checks.VerifyFixCommit("deadbeef", "fix/order-total-abc123"), Does.Contain("FAIL commit exists"));
        Assert.That(checks.VerifyFixCommit("--all", "fix/x"), Does.StartWith("Error"));
    }

    [Test]
    public void ReturnToMain_StashesLeftoversAndChecksOutMain()
    {
        Outside.Tool(GitAvailable(), "git is not installed");
        InitRepo();
        Git("switch", "-c", "fix/half-done");
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "unfinished");

        Assert.That(Tools(FeedWith()).ReturnToMain(), Is.Null);
        Assert.That(Git("rev-parse", "--abbrev-ref", "HEAD"), Is.EqualTo("main"));
        Assert.That(Git("stash", "list"), Does.Contain("uncommitted changes left on fix/half-done"));
    }

    private void InitRepo()
    {
        Git("init", "-b", "main");
        Git("config", "user.email", "test@localhost");
        Git("config", "user.name", "test");
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "one");
        File.WriteAllText(Path.Combine(_dir, ".gitignore"),
            "errors.jsonl\nstate/\n"); // like the service's ignored logs/
        Git("add", ".");
        Git("commit", "-m", "initial");
    }

    private JsonLinesErrorFeed FeedWith(params string[] events)
    {
        string log = Path.Combine(_dir, "errors.jsonl");
        File.WriteAllLines(log, events);
        var feed = new JsonLinesErrorFeed(log, Path.Combine(_dir, "state"));
        feed.ReadNew();
        return feed;
    }

    private static readonly HashSet<string> Methods = ["get-order", "order-total", "refund"];

    /// <summary>The orchestrator's settings over this test's repository.</summary>
    private OrchestratorOptions Options() => new()
    {
        ServiceRoot = _dir,
        StateDirectory = Path.Combine(_dir, "state"),
        SkillsDirectory = Path.Combine(AppContext.BaseDirectory, "Skills"),
    };

    private CommsLog Comms() => new(Path.Combine(_dir, "state", "agent-comms.md"));

    private ServiceTools Tools(JsonLinesErrorFeed feed)
    {
        var client = new LouisAgentClient(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1") }, null);
        return new ServiceTools(Options(), feed, client, Methods, Comms());
    }

    /// <summary>The checks of a fix, over the same repository and methods.</summary>
    private FixChecks Checks() => new(Options(), Methods, Comms());

    private string Git(params string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = _dir,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
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
            using var process =
                Process.Start(new ProcessStartInfo("git", "--version") { RedirectStandardOutput = true })!;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
