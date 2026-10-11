using LouisAgent.Core.Skills;
using louis_agent.core.tools;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Pricing;
using LouisAgent.Core.Llm;

namespace LouisAgent.Core.Tests;

// F3-S1 (step 1): a turn's scope collects its requests' records, as the ledger stored them (with their cost), so a host
// can show what the turn used.
public class UsageScopeTotalsTests
{
    private const string Prices = """
        {
          "as_of": "2026-10-10",
          "currency": "USD",
          "models": {
            "claude-haiku-5-5": { "input": 1.00, "output": 5.00, "cache_read": 0.10, "cache_write_5m": 1.25, "cache_write_1h": 2.00 }
          }
        }
        """;

    private static UsageRecord Record(long input, decimal? cost = null) => new(
        At: DateTimeOffset.UtcNow, Round: 1, Purpose: UsagePurpose.Turn, Host: null, Session: null, Turn: null, Task: null,
        Run: null, Service: null, Model: "claude-haiku-5-5", Tokens: new UsageTokens(input, 0, 0, 10, null), DurationMs: 1,
        Stop: "stop", Cost: cost is null ? null : new UsageCost("USD", cost.Value, "2026-10-10"));

    private static ChatResponse Reply(string text) =>
        new(new ChatMessage(ChatRole.Assistant, text))
        {
            Usage = new UsageDetails { InputTokenCount = 1_000, OutputTokenCount = 100 },
            ModelId = "claude-haiku-5-5",
        };

    [Test]
    public void Add_NestedScope_CountsInTheOuterScopeToo()
    {
        using var turn = UsageScope.Begin();
        turn.Add(Record(100));
        using (var summary = UsageScope.Begin(UsagePurpose.Summary))
        {
            summary.Add(Record(20));
            Assert.That(summary.Records, Has.Count.EqualTo(1), "the summary's own records");
        }

        Assert.That(turn.Records.Select(r => r.Tokens.Input), Is.EqualTo(new long?[] { 100, 20 }));
    }

    [Test]
    public void Add_NextTurn_DoesNotSeeThePreviousTurnsRecords()
    {
        using (var first = UsageScope.Begin()) first.Add(Record(100));

        using var second = UsageScope.Begin();
        Assert.That(second.Records, Is.Empty);
    }

    [Test]
    public void Records_IsASnapshot()
    {
        // A host may read the totals while a request of the same turn is still finishing.
        using var turn = UsageScope.Begin();
        turn.Add(Record(1));
        var before = turn.Records;

        turn.Add(Record(2));

        Assert.That(before, Has.Count.EqualTo(1));
    }

    [Test]
    public void Add_Concurrent_KeepsEveryRecord()
    {
        using var turn = UsageScope.Begin();

        Parallel.For(0, 1000, _ => turn.Add(Record(1)));

        Assert.That(turn.Records, Has.Count.EqualTo(1000));
    }

    [Test]
    public void Total_SumsTokensAndCostLikeALedgerReport()
    {
        using var turn = UsageScope.Begin();
        turn.Add(Record(100, 0.001m));
        turn.Add(Record(200, 0.002m));
        turn.Add(Record(300));

        Assert.That(turn.Total(), Is.EqualTo(UsageReport.Total(UsageScope.TotalKey, turn.Records)));
        Assert.That((turn.Total().Requests, turn.Total().Input, turn.Total().Cost, turn.Total().Unpriced),
            Is.EqualTo((3, 600L, (decimal?)0.003m, 1)));
    }

    [Test]
    public void Total_NoRequests_IsEmptyWithUnknownCost()
    {
        using var turn = UsageScope.Begin();

        Assert.That((turn.Total().Requests, turn.Total().Cost), Is.EqualTo((0, (decimal?)null)));
    }

    [Test]
    public async Task Recorder_AddsThePricedRecordToTheScope()
    {
        // The cost is added after the recorder (PricingUsageSink); the scope must get the record with it.
        string dir = Directory.CreateTempSubdirectory("scope-totals-").FullName;
        try
        {
            string prices = Path.Combine(dir, "prices.json");
            File.WriteAllText(prices, Prices);
            var ledger = new ListUsageSink();
            using var client = new UsageRecordingChatClient(
                new FakeChatClient(_ => Reply("hi")), new PricingUsageSink(ledger, PriceTable.Load(prices)));

            using var turn = UsageScope.Begin();
            await client.GetResponseAsync("hi");

            // 1,000 input at $1/M + 100 output at $5/M.
            Assert.That(turn.Records.Single().Cost!.Amount, Is.EqualTo(0.0015m));
            Assert.That(turn.Records.Single(), Is.EqualTo(ledger.Records.Single()), "the same record the ledger stored");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Recorder_StreamingRequest_IsAddedWhenItEnds()
    {
        using var client = new UsageRecordingChatClient(new FakeChatClient(_ => Reply("hi")), new ListUsageSink());

        using var turn = UsageScope.Begin();
        await client.GetStreamingResponseAsync("hi").ToListAsync();

        Assert.That(turn.Records, Has.Count.EqualTo(1));
    }

    [Test]
    public void Recorder_CancelledStream_StillCounts()
    {
        // Whatever a cancelled request used was still charged.
        using var client = new UsageRecordingChatClient(new FailingStreamClient(new OperationCanceledException()), new ListUsageSink());

        using var turn = UsageScope.Begin();
        Assert.ThrowsAsync<OperationCanceledException>(async () => await client.GetStreamingResponseAsync("hi").ToListAsync());

        Assert.That(turn.Records.Single().Stop, Is.EqualTo(UsageRecordingChatClient.Cancelled));
    }

    [Test]
    public void Recorder_ProviderError_AddsNothing()
    {
        using var client = new UsageRecordingChatClient(new FailingStreamClient(new HttpRequestException("529 overloaded")), new ListUsageSink());

        using var turn = UsageScope.Begin();
        Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetStreamingResponseAsync("hi").ToListAsync());

        Assert.That(turn.Records, Is.Empty);
    }

    [Test]
    public async Task Engine_TurnTotals_IncludeEveryRoundAndTheSummary_NotOtherTurns()
    {
        string relative = $"big-{Guid.NewGuid():N}.txt";
        string path = Path.Combine(TestPaths.RepoRoot, relative);
        File.WriteAllText(path, new string('x', AgentEngine.MaxToolResultChars + 1));
        try
        {
            var client = new FakeChatClient(
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [
                    new FunctionCallContent("call_1", "ReadWorkspaceFile",
                        new Dictionary<string, object?> { ["relativePath"] = relative })
                ])),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "a file full of x")),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "It's all x.")),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Yes.")));
            var engine = new AgentEngine(
                new MarkdownSkillProvider(Path.Combine(TestPaths.RepoRoot, "Skills", "HelloWorld.md")), client, TestPaths.Agent(),
                usageSink: new ListUsageSink());
            var history = engine.NewHistory();

            IReadOnlyList<UsageRecord> first, second;
            using (var turn = UsageScope.Begin(turn: 1))
            {
                await engine.ProcessPromptAsync(history, "what is in the file?");
                first = turn.Records;
            }
            using (var turn = UsageScope.Begin(turn: 2))
            {
                await engine.ProcessPromptAsync(history, "are you sure?");
                second = turn.Records;
            }

            Assert.That(first.Select(r => r.Purpose), Is.EqualTo(new[] { UsagePurpose.Turn, UsagePurpose.Summary, UsagePurpose.Turn }));
            Assert.That(second, Has.Count.EqualTo(1));
        }
        finally
        {
            File.Delete(path);
        }
    }
}