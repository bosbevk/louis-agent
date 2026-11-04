using System.Text.Json;
using LouisAgent.Core.Usage.Reports;
using LouisAgent.Orchestrator.Comms;

namespace LouisAgent.Orchestrator.Tests.Comms;

/// <summary>The usage figures in the orchestrator's jsonl files.</summary>
public class UsageLineTests
{
    /// <summary>They use louis-agent.api's field names, so every file reads alike.</summary>
    [Test]
    public void UsesTheApisFieldNames()
    {
        var usage = new UsageTotal("total", 7, 5_500, 0, 30_000, 700, 0.019m, 0);

        string json = JsonSerializer.Serialize(UsageLine.Of(usage, "USD"));

        Assert.That(json,
            Is.EqualTo(
                "{\"requests\":7,\"input_tokens\":5500,\"cache_creation_input_tokens\":0," +
                "\"cache_read_input_tokens\":30000,\"output_tokens\":700,\"cost\":0.019,\"currency\":\"USD\"," +
                "\"unpriced\":0}"));
    }

    /// <summary>Nothing measured is no line at all, and nothing priced names no currency.</summary>
    [Test]
    public void Unmeasured_IsNull_AndUnpriced_HasNoCurrency()
    {
        var unpriced = new UsageTotal("total", 1, 10, 0, 0, 5, null, 1);

        Assert.That(UsageLine.Of(null, "USD"), Is.Null);
        Assert.That(UsageLine.Of(unpriced, "USD")!.Currency, Is.Null);
    }
}
