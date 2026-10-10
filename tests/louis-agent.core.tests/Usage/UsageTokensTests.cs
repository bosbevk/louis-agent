using louis_agent.core.usage;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

// F1-S1: counts mapped from UsageDetails; missing counts stay null. Numbers are from the F1 step 1 spike.
public class UsageTokensTests
{
    [Test]
    public void From_NullUsage_AllCountsNull()
    {
        Assert.Ignore("TODO F1 step 3");
    }

    [Test]
    public void From_CacheRead_SubtractsCachedTokensFromInput()
    {
        // InputTokenCount 10,266, CachedInputTokenCount 10,227 => Input 39, CacheRead 10,227, CacheWrite null.
        Assert.Ignore("TODO F1 step 3");
    }

    [Test]
    public void From_CacheWrite_ReadsCacheCreationKeyAndSubtractsIt()
    {
        // InputTokenCount 10,266, AdditionalCounts["CacheCreationInputTokens"] 10,227 => Input 39, CacheWrite 10,227.
        Assert.Ignore("TODO F1 step 3");
    }

    [Test]
    public void From_NoReasoningCount_ReasoningStaysNull()
    {
        Assert.Ignore("TODO F1 step 3");
    }

    [Test]
    public void From_ProviderReportsNothing_CountsAreNullNotZero()
    {
        // new UsageDetails() with every property unset, as some Ollama models return.
        _ = new UsageDetails();
        Assert.Ignore("TODO F1 step 3");
    }
}