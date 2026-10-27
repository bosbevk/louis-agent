using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Usage.Reports;
using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.Clock;
using Microsoft.Extensions.AI;

using static LouisAgent.Core.Tests.Tools.Agents.FakePeer;

namespace LouisAgent.Core.Tests.Tools.Agents;

// F5, FX5, FX6: what a sub-session used is one ledger row in the parent's turn, failed or not; a timeout leaves none.
public class SpecialistToolsRollUpTests
{
    [Test]
    public async Task ASubSessionsUsage_IsRolledUpIntoTheParentTurn_AsOneLedgerRow()
    {
        // F5 follow-up: the specialist's 3 requests count in the parent's turn (its usage line, chat total, /usage,
        // GET /v1/sessions/{id} all total the scope) and leave one sub_session row in the parent's ledger.
        var sink = new ListUsageSink();
        var handler = Peer();

        UsageTotal total;
        using (var scope = UsageScope.Begin(host: "api", session: "sess_parent", turn: 2))
        {
            await ToolsOn(handler, sink: sink).StartSubSession([CoreToolset.Files.Name(), CoreToolset.Git.Name()], "Fix order 1006.", agent: "coder");
            total = scope.Total();
        }

        Assert.That((total.Requests, total.Input, total.CacheRead, total.Output, total.Cost), Is.EqualTo((3, 1000L, 9000L, 200L, (decimal?)0.0123m)));
        UsageRecord row = sink.Records.Single();
        Assert.That((row.Purpose, row.Model, row.Stop, row.Round, row.Requests), Is.EqualTo(("sub_session", "sub-session:coder", "sub_session", 0, (int?)3)));
        Assert.That((row.Host, row.Session, row.Turn), Is.EqualTo(("api", "sess_parent", (int?)2)));
        Assert.That(row.Cost, Is.EqualTo(new UsageCost("USD", 0.0123m, null)), "what the child was charged, not a display amount");
        Assert.That(row.Tokens, Is.EqualTo(new UsageTokens(1000, 0, 9000, 200, null)));
    }

    [Test]
    public async Task TheFollowUpAnswer_IsASecondRollUp()
    {
        // FX11's follow-up is a second reply in the same sub-session: its 11 + 3 requests are two rows and one total.
        var sink = new ListUsageSink();

        UsageTotal total;
        using (var scope = UsageScope.Begin(session: "sess_parent", turn: 1))
        {
            await ToolsOn(Peer(Replies(EmptyStream, AnswerStream)), sink: sink).StartSubSession([CoreToolset.Files.Name()], "x", agent: "coder");
            total = scope.Total();
        }

        Assert.That(sink.Records.Select(r => r.Requests), Is.EqualTo(new int?[] { 11, 3 }));
        Assert.That((total.Requests, total.Cost), Is.EqualTo((14, (decimal?)0.0204m)));
    }

    [Test]
    public async Task AFailedChild_StillRollsUpWhatItWasCharged()
    {
        var sink = new ListUsageSink();

        string result;
        using (var scope = UsageScope.Begin(session: "sess_parent", turn: 1))
            result = await ToolsOn(Peer(Replies(FailedStream)), sink: sink).StartSubSession([CoreToolset.Files.Name()], "x", agent: "coder");

        Assert.That(result, Does.StartWith("Error").And.Contain("the model refused"));
        Assert.That((sink.Records.Single().Requests, sink.Records.Single().Cost?.Amount), Is.EqualTo(((int?)2, (decimal?)0.002m)));
    }

    [Test]
    public async Task ATimeout_RollsUpNothing()
    {
        // No usage arrived, so there is nothing to count: never a zero record (trap 4).
        var sink = new ListUsageSink();

        using (UsageScope.Begin(session: "sess_parent", turn: 1))
            await new SpecialistTools(Peers, null, new HttpClient(new SlowPeer()), TimeSpan.FromMilliseconds(200), usageSink: sink)
                .StartSubSession([CoreToolset.Files.Name()], "x", agent: "coder");

        Assert.That(sink.Records, Is.Empty);
    }

    [Test]
    public async Task ThroughTheEngine_TheLedgerHasTheParentsRowsAndOneRollUp_AndNoneOfTheChilds()
    {
        // Step 3: a turn that delegates writes its own two requests and one roll-up, in the order they happened; the
        // specialist's own rows are written where it ran. The turn's total is own + child, with one adder.
        var sink = new ListUsageSink();
        var tools = new SpecialistTools(Peers, null, new HttpClient(Peer()), usageSink: sink);
        static ChatResponse Reply(params AIContent[] contents) => new(new ChatMessage(ChatRole.Assistant, contents))
        {
            Usage = new UsageDetails { InputTokenCount = 1_000, OutputTokenCount = 100 },
        };
        var engine = new AgentEngine(
            new MarkdownSkillProvider(Path.Combine(TestPaths.RepoRoot, "Skills", "HelloWorld.md")),
            new FakeChatClient(
                _ => Reply(new FunctionCallContent("call_1", "StartSubSession",
                    new Dictionary<string, object?> { ["toolsets"] = new[] { CoreToolset.Files.Name() }, ["message"] = "Fix order 1006.", ["agent"] = "coder" })),
                _ => Reply(new TextContent("Done."))),
            TestPaths.Agent(), toolsets: [tools], usageSink: sink);

        UsageTotal total;
        using (var scope = UsageScope.Begin(host: "api", session: "sess_parent", turn: 1))
        {
            await engine.ProcessPromptAsync(engine.NewHistory(), "delegate this");
            total = scope.Total();
        }

        Assert.That(sink.Records.Select(r => r.Purpose), Is.EqualTo(new[] { "turn", "sub_session", "turn" }));
        Assert.That(sink.Records.Select(r => r.Session), Is.All.EqualTo("sess_parent"));
        Assert.That((total.Requests, total.Cost, total.Unpriced), Is.EqualTo((5, (decimal?)0.0123m, 2)), "1 + 3 + 1; the own rows are unpriced here");
    }

    [Test]
    public async Task OutsideATurn_TheRollUpIsStillALedgerRow()
    {
        // As a model request outside a scope is (FX5): the ledger records it with no host or session.
        var sink = new ListUsageSink();

        string result = await ToolsOn(Peer(), sink: sink).StartSubSession([CoreToolset.Files.Name()], "x", agent: "coder");

        Assert.That(result, Does.StartWith("Fixed the null check"));
        Assert.That((sink.Records.Single().Session, sink.Records.Single().Requests), Is.EqualTo(((string?)null, (int?)3)));
    }

    [Test]
    public async Task TheParentTurn_IsSentSoTheChildsUsageLinksToIt()
    {
        // Step 9: the child's records carry parent = "{session}#{turn}" of the turn that opened it.
        var handler = Peer();

        using (UsageScope.Begin(session: "sess_parent", turn: 4, tags: new UsageTags(null, "run-1", "order-service")))
            await ToolsOn(handler).StartSubSession([CoreToolset.Files.Name()], "x", agent: "coder");

        using var create = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.That(create.RootElement.GetProperty("parent").GetString(), Is.EqualTo("sess_parent#4"));
        Assert.That(create.RootElement.GetProperty("run").GetString(), Is.EqualTo("run-1"));
    }
}
