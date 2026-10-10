using louis_agent.core.providers;
using louis_agent.core.tools;
using louis_agent.core.usage;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

// F3 step 3: a host session sums its turns for the usage line's session total and for /usage (F3-S3).
public class UsageSessionTests
{
    private static UsageRecord Record(decimal cost) => new(
        At: DateTimeOffset.UtcNow, Round: 1, Purpose: UsagePurpose.Turn, Host: "cli", Session: "s", Turn: 1, Task: null,
        Run: null, Service: null, Model: "claude-haiku-5-5", Tokens: new UsageTokens(1_000, 0, 0, 100, null), DurationMs: 1,
        Stop: "stop", Cost: new UsageCost("USD", cost, "2026-10-10"));

    [Test]
    public void EndTurn_LineHasTheTurnAndTheSessionSoFar()
    {
        var session = new UsageSession();

        string? first = session.EndTurn([Record(0.01m)]);
        string? second = session.EndTurn([Record(0.02m), Record(0.02m)]);

        Assert.That(first, Is.EqualTo("turn: 1 request · 1.1k tokens · $0.01 · session $0.01"));
        Assert.That(second, Is.EqualTo("turn: 2 requests · 2.2k tokens · $0.04 · session $0.05"));
        Assert.That(session.Records, Has.Count.EqualTo(3));
    }

    [Test]
    public void EndTurn_NoRequests_NoLineAndNothingAdded()
    {
        var session = new UsageSession();

        Assert.That(session.EndTurn([]), Is.Null);
        Assert.That(session.Records, Is.Empty);
    }

    [Test]
    public void Records_IsASnapshot()
    {
        var session = new UsageSession();
        session.EndTurn([Record(0.01m)]);
        var before = session.Records;

        session.EndTurn([Record(0.01m)]);

        Assert.That(before, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task TwoEngineTurns_SessionTotalCoversBoth()
    {
        // How the CLI uses it: one scope per turn, the session collects each turn's records when it ends.
        static ChatResponse Reply() => new(new ChatMessage(ChatRole.Assistant, "ok"))
        {
            Usage = new UsageDetails { InputTokenCount = 1_000, OutputTokenCount = 100 },
        };
        var engine = new AgentEngine(
            new MarkdownSkillProvider(Path.Combine(TestPaths.RepoRoot, "Skills", "HelloWorld.md")),
            new FakeChatClient(_ => Reply(), _ => Reply()), TestPaths.Agent(), usageSink: new ListUsageSink());
        var history = engine.NewHistory();
        var session = new UsageSession();

        for (int turn = 1; turn <= 2; turn++)
        {
            using var usage = UsageScope.Begin(host: "cli", session: "s", turn: turn);
            await engine.ProcessPromptAsync(history, "hi");
            session.EndTurn(usage.Records);
        }

        Assert.That(session.Records.Select(r => r.Turn), Is.EqualTo(new int?[] { 1, 2 }));
        Assert.That(engine.HandleUserCommand("/usage", session.Records), Does.StartWith("Session usage: 2 requests · 2.2k tokens"));
    }

    [Test]
    public void RecordsUsage_OnlyWithASink()
    {
        var skills = new MarkdownSkillProvider(Path.Combine(TestPaths.RepoRoot, "Skills", "HelloWorld.md"));

        Assert.That(new AgentEngine(skills, new FakeChatClient(), TestPaths.Agent(), usageSink: new ListUsageSink()).RecordsUsage, Is.True);
        Assert.That(new AgentEngine(skills, new FakeChatClient(), TestPaths.Agent()).RecordsUsage, Is.False);
    }
}