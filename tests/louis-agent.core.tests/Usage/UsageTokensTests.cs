using louis_agent.core.usage;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

// F1-S1: counts mapped from UsageDetails; missing counts stay null. Numbers are from the F1 step 1 spike.
public class UsageTokensTests
{
    [Test]
    public void From_NullUsage_AllCountsNull()
    {
        Assert.That(UsageTokens.From(null), Is.EqualTo(new UsageTokens(null, null, null, null, null)));
    }

    [Test]
    public void From_NoCache_InputUnchanged()
    {
        var usage = new UsageDetails { InputTokenCount = 10_266, CachedInputTokenCount = 0, OutputTokenCount = 277 };

        Assert.That(UsageTokens.From(usage), Is.EqualTo(new UsageTokens(10_266, null, 0, 277, null)));
    }

    [Test]
    public void From_CacheRead_SubtractsCachedTokensFromInput()
    {
        var usage = new UsageDetails { InputTokenCount = 10_266, CachedInputTokenCount = 10_227, OutputTokenCount = 206 };

        Assert.That(UsageTokens.From(usage), Is.EqualTo(new UsageTokens(39, null, 10_227, 206, null)));
    }

    [Test]
    public void From_CacheWrite_ReadsCacheCreationKeyAndSubtractsIt()
    {
        var usage = new UsageDetails
        {
            InputTokenCount = 10_266,
            CachedInputTokenCount = 0,
            OutputTokenCount = 144,
            AdditionalCounts = new AdditionalPropertiesDictionary<long> { ["CacheCreationInputTokens"] = 10_227 },
        };

        Assert.That(UsageTokens.From(usage), Is.EqualTo(new UsageTokens(39, 10_227, 0, 144, null)));
    }

    [Test]
    public void From_ReasoningCount_IsKept()
    {
        var usage = new UsageDetails { InputTokenCount = 100, OutputTokenCount = 50, ReasoningTokenCount = 30 };

        Assert.That(UsageTokens.From(usage).Reasoning, Is.EqualTo(30));
    }

    [Test]
    public void From_ProviderReportsNothing_CountsAreNullNotZero()
    {
        // Some Ollama models return usage with every count unset.
        Assert.That(UsageTokens.From(new UsageDetails()), Is.EqualTo(new UsageTokens(null, null, null, null, null)));
    }
}
