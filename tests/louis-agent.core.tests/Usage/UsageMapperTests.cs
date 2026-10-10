using louis_agent.core.config;
using louis_agent.core.providers;
using louis_agent.core.usage;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

// F1-S1: counts mapped from UsageDetails; missing counts stay null. Numbers are from the F1 step 1 spike.
public class UsageMapperTests
{
    private static readonly IUsageMapper Standard = new StandardUsageMapper();
    private static readonly IUsageMapper Anthropic = new AnthropicUsageMapper();

    private static UsageDetails AnthropicCacheWrite() => new()
    {
        InputTokenCount = 10_266,
        CachedInputTokenCount = 0,
        OutputTokenCount = 144,
        AdditionalCounts = new AdditionalPropertiesDictionary<long> { ["CacheCreationInputTokens"] = 10_227 },
    };

    [Test]
    public void Map_NullUsage_AllCountsNull()
    {
        Assert.That(Standard.Map(null), Is.EqualTo(new UsageTokens(null, null, null, null, null)));
        Assert.That(Anthropic.Map(null), Is.EqualTo(new UsageTokens(null, null, null, null, null)));
    }

    [Test]
    public void Map_NoCache_InputUnchanged()
    {
        var usage = new UsageDetails { InputTokenCount = 10_266, CachedInputTokenCount = 0, OutputTokenCount = 277 };

        Assert.That(Standard.Map(usage), Is.EqualTo(new UsageTokens(10_266, null, 0, 277, null)));
    }

    [Test]
    public void Map_CacheRead_SubtractsCachedTokensFromInput()
    {
        // The Microsoft.Extensions.AI contract: cached tokens are part of InputTokenCount. Holds for every provider.
        var usage = new UsageDetails { InputTokenCount = 10_266, CachedInputTokenCount = 10_227, OutputTokenCount = 206 };

        Assert.That(Standard.Map(usage), Is.EqualTo(new UsageTokens(39, null, 10_227, 206, null)));
        Assert.That(Anthropic.Map(usage), Is.EqualTo(new UsageTokens(39, null, 10_227, 206, null)));
    }

    [Test]
    public void Map_Anthropic_ReadsCacheCreationKeyAndSubtractsIt()
    {
        Assert.That(Anthropic.Map(AnthropicCacheWrite()), Is.EqualTo(new UsageTokens(39, 10_227, 0, 144, null)));
    }

    [Test]
    public void Map_Standard_IgnoresProviderSpecificKeys()
    {
        // Without a provider mapper the write isn't recognised, so it stays in the input rather than being guessed at.
        Assert.That(Standard.Map(AnthropicCacheWrite()), Is.EqualTo(new UsageTokens(10_266, null, 0, 144, null)));
    }

    [Test]
    public void Map_ReasoningCount_IsKept()
    {
        var usage = new UsageDetails { InputTokenCount = 100, OutputTokenCount = 50, ReasoningTokenCount = 30 };

        Assert.That(Standard.Map(usage).Reasoning, Is.EqualTo(30));
    }

    [Test]
    public void Map_ProviderReportsNothing_CountsAreNullNotZero()
    {
        // Some Ollama models return usage with every count unset.
        Assert.That(Standard.Map(new UsageDetails()), Is.EqualTo(new UsageTokens(null, null, null, null, null)));
    }

    [TestCase(LlmOptions.Anthropic, typeof(AnthropicUsageMapper))]
    [TestCase("ANTHROPIC", typeof(AnthropicUsageMapper))]
    [TestCase(LlmOptions.Ollama, typeof(StandardUsageMapper))]
    [TestCase(LlmOptions.OpenAiCompatible, typeof(StandardUsageMapper))]
    public void CreateUsageMapper_PicksTheProvidersMapper(string provider, Type expected)
    {
        var mapper = new LlmClientFactory().CreateUsageMapper(new LlmOptions { Provider = provider, Model = "m" });

        Assert.That(mapper, Is.TypeOf(expected));
    }

    [Test]
    public async Task Recorder_UsesTheMapperItIsGiven()
    {
        var sink = new ListUsageSink();
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "hi")) { Usage = AnthropicCacheWrite() };
        using var client = new UsageRecordingChatClient(new FakeChatClient(_ => response), sink, mapper: Anthropic);

        await client.GetResponseAsync("hi");

        Assert.That(sink.Records.Single().Tokens.CacheWrite, Is.EqualTo(10_227));
    }
}
