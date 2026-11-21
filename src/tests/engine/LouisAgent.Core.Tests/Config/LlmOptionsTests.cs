using LouisAgent.Core.Config;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Llm;

namespace LouisAgent.Core.Tests.Config;

public class LlmOptionsTests
{
    private static Func<string, string?> Env(params (string, string)[] values)
    {
        var dict = values.ToDictionary(v => v.Item1, v => v.Item2);
        return name => dict.GetValueOrDefault(name);
    }

    [Test]
    public void FromEnvironment_ReadsExplicitSettings()
    {
        var o = LlmOptions.FromEnvironment(Env(
            ("LLM_PROVIDER", "Ollama"), ("LLM_MODEL", "qwen2.5-coder"), ("LLM_ENDPOINT", "http://h:1"),
            ("LLM_API_KEY", "k"), ("LLM_SUPPORTS_TOOLS", "false")));

        Assert.That(o.Provider, Is.EqualTo("ollama"));
        Assert.That(o.Model, Is.EqualTo("qwen2.5-coder"));
        Assert.That(o.Endpoint, Is.EqualTo("http://h:1"));
        Assert.That(o.ApiKey, Is.EqualTo("k"));
        Assert.That(o.ResolveSupportsTools(), Is.False);
    }

    [Test]
    public void FromEnvironment_InfersProviderFromModelWhenUnset()
    {
        Assert.That(LlmOptions.FromEnvironment(Env(("LLM_MODEL", "claude-haiku-4-5-20251001"))).Provider,
            Is.EqualTo("anthropic"));
        Assert.That(LlmOptions.FromEnvironment(Env(("LLM_MODEL", "llama3.2"))).Provider, Is.EqualTo("ollama"));
        Assert.That(LlmOptions.FromEnvironment(Env()).Provider, Is.EqualTo("anthropic"));
    }

    [Test]
    public void FromEnvironment_AnthropicFallsBackToAnthropicApiKey()
    {
        var o = LlmOptions.FromEnvironment(Env(("LLM_PROVIDER", "anthropic"), ("ANTHROPIC_API_KEY", "a")));
        Assert.That(o.ApiKey, Is.EqualTo("a"));
    }

    [Test]
    public void ToString_NeverContainsTheApiKey()
    {
        var o = new LlmOptions { ApiKey = "super-secret" };
        Assert.That(o.ToString(), Does.Not.Contain("super-secret"));
    }

    [TestCase("ollama", "llama2", null, false)]
    [TestCase("ollama", "deepseek-r1:8b", null, false)]
    [TestCase("ollama", "qwen2.5-coder", null, false)]
    [TestCase("ollama", "llama3.1", null, true)]
    [TestCase("ollama", "llama2", true, true)]
    [TestCase("ollama", "qwen2.5-coder", true, true)]
    [TestCase("anthropic", "claude-haiku-4-5-20251001", null, true)]
    public void ResolveSupportsTools_UsesOverrideThenModelHeuristic(string provider, string model, bool? over,
        bool expected)
    {
        var o = new LlmOptions
        {
            Provider = provider,
            Model = model,
            SupportsTools = over
        };
        Assert.That(o.ResolveSupportsTools(), Is.EqualTo(expected));
    }
}

public class ThinkingTests
{
    [TestCase("anthropic", null, ReasoningEffort.Medium)]
    [TestCase("ollama", null, null)]
    [TestCase("anthropic", "off", null)]
    [TestCase("anthropic", "HIGH", ReasoningEffort.High)]
    [TestCase("ollama", "low", ReasoningEffort.Low)]
    public void ResolveThinking_DefaultsPerProviderAndHonoursSetting(string provider, string? setting,
        ReasoningEffort? expected)
    {
        var o = new LlmOptions
        {
            Provider = provider,
            Thinking = setting
        };
        Assert.That(o.ResolveThinking(), Is.EqualTo(expected));
    }

    [Test]
    public void ResolveThinking_UnknownValue_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => new LlmOptions { Thinking = "lots" }.ResolveThinking());
        Assert.That(ex!.Message, Does.Contain("LLM_THINKING"));
    }

    // F4 step 3: the prompt-cache settings.
    [TestCase(null, true)]
    [TestCase("on", true)]
    [TestCase("ON", true)]
    [TestCase("off", false)]
    [TestCase("false", false)]
    public void ResolveCache_OnUnlessOff(string? setting, bool expected) =>
        Assert.That(new LlmOptions { Cache = setting }.ResolveCache(), Is.EqualTo(expected));

    [TestCase(null, CacheLifetime.FiveMinutes)]
    [TestCase("5m", CacheLifetime.FiveMinutes)]
    [TestCase("1h", CacheLifetime.OneHour)]
    [TestCase("1H", CacheLifetime.OneHour)]
    public void ResolveCacheTtl_FiveMinutesUnlessAnHour(string? setting, CacheLifetime expected) =>
        Assert.That(new LlmOptions { CacheTtl = setting }.ResolveCacheTtl(), Is.EqualTo(expected));

    [Test]
    public void CacheSettings_BadValues_SayWhatIsAllowed()
    {
        Assert.That(() => new LlmOptions { Cache = "maybe" }.ResolveCache(),
            Throws.ArgumentException.With.Message.Contain("LLM_CACHE 'maybe'").And.Message.Contain("on or off"));
        Assert.That(() => new LlmOptions { CacheTtl = "10m" }.ResolveCacheTtl(),
            Throws.ArgumentException.With.Message.Contain("LLM_CACHE_TTL '10m'").And.Message.Contain("5m or 1h"));
    }

    [Test]
    public void CacheSettings_AreReadFromTheEnvironment()
    {
        var o = LlmOptions.FromEnvironment(name =>
            name switch { "LLM_CACHE" => "off", "LLM_CACHE_TTL" => "1h", _ => null });

        Assert.That((o.ResolveCache(), o.ResolveCacheTtl()), Is.EqualTo((false, CacheLifetime.OneHour)));
    }
}
