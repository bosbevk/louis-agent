using LouisAgent.Core.Llm.Replay;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Grading;
using Microsoft.Extensions.AI;

namespace LouisAgent.Evals.Tests.Grading;

/// <summary>
/// F126.3.4: a judge's mark comes back with what its call cost, read from its own usage scope: the ledger's price
/// when the model records to one, none when it doesn't, and a known 0 when the answer was played back.
/// </summary>
public class JudgeCostTests
{
    private const string Model = "judge-model";

    private static readonly IReadOnlyList<TurnTranscript> Conversation =
        [new("What is louis-agent?", "An AI agent framework in C#.", [], TimeSpan.FromSeconds(4))];

    /// <summary>A judge's model that records to a ledger priced by this table.</summary>
    private static readonly PriceTable Prices = new()
    {
        AsOf = "2026-11-23",
        Currency = "USD",
        Models = new Dictionary<string, ModelPrice>
        {
            [Model] = new()
            {
                Input = 1m,
                Output = 5m,
                CacheRead = 0.1m,
                CacheWrite5m = 1.25m,
                CacheWrite1h = 2m
            }
        }
    };

    /// <summary>A mark from a model that records to a ledger costs what the ledger priced its call at.</summary>
    [Test]
    public async Task AMark_CostsWhatItsCallWasRecordedWith()
    {
        var ledger = new ListUsageSink();
        using var model = new UsageRecordingChatClient(Answering(replayed: false), new PricingUsageSink(ledger, Prices),
            Model);

        var judged = await new Judge(model).MarkAsync("Says it is in C#", Conversation);

        Assert.That(ledger.Records.Single().Cost, Is.Not.Null);
        Assert.That(judged.Cost, Is.EqualTo(ledger.Records.Single().Cost!.Amount));
        Assert.That(judged.Cost, Is.GreaterThan(0m));
    }

    /// <summary>A mark from a model with no ledger has no known cost, never 0.</summary>
    [Test]
    public async Task AMark_WithNoLedger_HasNoKnownCost() =>
        Assert.That((await new Judge(Answering(replayed: false)).MarkAsync("Says it is in C#", Conversation)).Cost,
            Is.Null);

    /// <summary>A mark played back from a recording cost nothing: a known 0.</summary>
    [Test]
    public async Task AMarkPlayedBack_CostsAKnownZero()
    {
        var ledger = new ListUsageSink();
        using var model = new UsageRecordingChatClient(Answering(replayed: true), new PricingUsageSink(ledger, Prices),
            Model);

        var judged = await new Judge(model).MarkAsync("Says it is in C#", Conversation);

        Assert.That(judged.Cost, Is.EqualTo(0m));
    }

    /// <summary>A judge's model that marks the criterion met, with its usage, tagged if played back.</summary>
    private static FakeChatClient Answering(bool replayed) =>
        new(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, """{"reason":"It says so.","met":true}"""))
        {
            ModelId = Model,
            Usage = new UsageDetails
            {
                InputTokenCount = 1000,
                OutputTokenCount = 100
            },
            AdditionalProperties =
                replayed ? new AdditionalPropertiesDictionary { [ModelReplayChatClient.HitTag] = true } : null
        });
}
