using Anthropic.Models.Messages;
using louis_agent.core.config;
using louis_agent.core.tools;
using louis_agent.core.providers;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

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
        Assert.That(LlmOptions.FromEnvironment(Env(("LLM_MODEL", "claude-haiku-4-5-20251001"))).Provider, Is.EqualTo("anthropic"));
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
    public void ResolveSupportsTools_UsesOverrideThenModelHeuristic(string provider, string model, bool? over, bool expected)
    {
        var o = new LlmOptions { Provider = provider, Model = model, SupportsTools = over };
        Assert.That(o.ResolveSupportsTools(), Is.EqualTo(expected));
    }
}

public class LlmClientFactoryTests
{
    private readonly LlmClientFactory _factory = new();

    [Test]
    public void Create_Ollama_ReturnsOllamaClient()
    {
        using var client = _factory.Create(new LlmOptions { Provider = "ollama", Model = "llama3.2" });
        Assert.That(client.GetService<ChatClientMetadata>()!.ProviderName, Is.EqualTo("ollama"));
    }

    [Test]
    public void Create_Anthropic_ReturnsClientWhenKeyPresent()
    {
        using var client = _factory.Create(new LlmOptions { Provider = "anthropic", Model = "claude-haiku-4-5-20251001", ApiKey = "k" });
        Assert.That(client.GetService<ChatClientMetadata>()!.ProviderName, Does.Contain("anthropic").IgnoreCase);
    }

    [Test]
    public void Create_Anthropic_WithoutKey_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _factory.Create(new LlmOptions { Provider = "anthropic", Model = "m" }));
        Assert.That(ex!.Message, Does.Contain("ANTHROPIC_API_KEY"));
    }

    [Test]
    public void Create_OpenAiCompatible_RequiresEndpoint()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _factory.Create(new LlmOptions { Provider = "openai-compatible", Model = "m" }));

        using var client = _factory.Create(new LlmOptions { Provider = "openai-compatible", Model = "m", Endpoint = "http://localhost:1234/v1" });
        Assert.That(client, Is.Not.Null);
    }

    [Test]
    public void Create_UnknownProvider_ListsSupportedProviders()
    {
        var ex = Assert.Throws<ArgumentException>(() => _factory.Create(new LlmOptions { Provider = "bogus", Model = "m" }));
        Assert.That(ex!.Message, Does.Contain("anthropic").And.Contain("ollama").And.Contain("openai-compatible"));
    }

    // Additional comprehensive tests

    [Test]
    public void Create_AnthropicWithCustomEndpoint()
    {
        using var client = _factory.Create(new LlmOptions
        {
            Provider = "anthropic",
            Model = "claude-haiku-4-5-20251001",
            ApiKey = "test-key",
            Endpoint = "https://custom.anthropic.com"
        });

        Assert.That(client, Is.Not.Null);
    }

    [Test]
    public void Create_OllamaWithCustomEndpoint()
    {
        using var client = _factory.Create(new LlmOptions
        {
            Provider = "ollama",
            Model = "mistral",
            Endpoint = "http://localhost:11434"
        });

        Assert.That(client, Is.Not.Null);
    }

    [Test]
    public void Create_OpenAiCompatibleWithApiKey()
    {
        using var client = _factory.Create(new LlmOptions
        {
            Provider = "openai-compatible",
            Model = "gpt-4o-mini",
            Endpoint = "http://localhost:8000/v1",
            ApiKey = "test-key"
        });

        Assert.That(client, Is.Not.Null);
    }

    [Test]
    public void Create_WithoutModel_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _factory.Create(new LlmOptions
        {
            Provider = "anthropic",
            Model = null!,
            ApiKey = "test-key"
        }));
    }

    [Test]
    public void Create_WithEmptyModel_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _factory.Create(new LlmOptions
        {
            Provider = "anthropic",
            Model = "",
            ApiKey = "test-key"
        }));
    }

    [Test]
    public void Create_ProviderNameIsCaseInsensitive()
    {
        using var client = _factory.Create(new LlmOptions
        {
            Provider = "ANTHROPIC",
            Model = "claude-haiku-4-5-20251001",
            ApiKey = "test-key"
        });

        Assert.That(client, Is.Not.Null);
    }

    [Test]
    public void Create_MultipleCallsReturnDifferentInstances()
    {
        var options = new LlmOptions
        {
            Provider = "anthropic",
            Model = "claude-haiku-4-5-20251001",
            ApiKey = "test-key"
        };

        using var client1 = _factory.Create(options);
        using var client2 = _factory.Create(options);

        Assert.That(client1, Is.Not.SameAs(client2));
    }

    [Test]
    public void Create_AnthropicWithWhitespaceApiKey_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => _factory.Create(new LlmOptions
        {
            Provider = "anthropic",
            Model = "claude-haiku-4-5-20251001",
            ApiKey = "   "
        }));
    }

    [Test]
    public void Create_OpenAiCompatibleWithEmptyEndpoint_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => _factory.Create(new LlmOptions
        {
            Provider = "openai-compatible",
            Model = "gpt-4o-mini",
            Endpoint = "",
            ApiKey = "test-key"
        }));
    }

    [Test]
    public void Create_OpenAiCompatibleWithoutApiKey_UsesDefaultCredential()
    {
        using var client = _factory.Create(new LlmOptions
        {
            Provider = "openai-compatible",
            Model = "gpt-4o-mini",
            Endpoint = "http://localhost:8000/v1",
            ApiKey = string.Empty
        });

        Assert.That(client, Is.Not.Null);
    }

    [Test]
    public void Create_WithWhitespaceModel_Throws()
    {
        Assert.Throws<ArgumentException>(() => _factory.Create(new LlmOptions
        {
            Provider = "anthropic",
            Model = "   ",
            ApiKey = "test-key"
        }));
    }

    [Test]
    public void Create_OllamaDefaultsToLocalhost()
    {
        using var client = _factory.Create(new LlmOptions
        {
            Provider = "ollama",
            Model = "llama2",
            Endpoint = null
        });

        Assert.That(client, Is.Not.Null);
    }
}

public class ThinkingTests
{
    [TestCase("anthropic", null, ReasoningEffort.Medium)]
    [TestCase("ollama", null, null)]
    [TestCase("anthropic", "off", null)]
    [TestCase("anthropic", "HIGH", ReasoningEffort.High)]
    [TestCase("ollama", "low", ReasoningEffort.Low)]
    public void ResolveThinking_DefaultsPerProviderAndHonoursSetting(string provider, string? setting, ReasoningEffort? expected)
    {
        var o = new LlmOptions { Provider = provider, Thinking = setting };
        Assert.That(o.ResolveThinking(), Is.EqualTo(expected));
    }

    [Test]
    public void ResolveThinking_UnknownValue_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => new LlmOptions { Thinking = "lots" }.ResolveThinking());
        Assert.That(ex!.Message, Does.Contain("LLM_THINKING"));
    }

    [TestCase("claude-haiku-4-5-20251001", true)]
    [TestCase("claude-haiku-5-5", false)]
    [TestCase("claude-sonnet-4-5", true)]
    [TestCase("claude-opus-4-6", false)]
    [TestCase("claude-opus-5-5", false)]
    public void UsesThinkingBudget_OnlyForModelsBeforeAdaptiveThinking(string model, bool expected) =>
        Assert.That(LlmClientFactory.UsesThinkingBudget(model), Is.EqualTo(expected));

    [Test]
    public void WithThinkingBudget_ReplacesReasoningWithABudgetBelowMaxTokens()
    {
        var options = new ChatOptions
        {
            MaxOutputTokens = 16_000,
            Reasoning = new ReasoningOptions { Effort = ReasoningEffort.Medium, Output = ReasoningOutput.Full },
        };

        var result = LlmClientFactory.WithThinkingBudget(options, "claude-haiku-4-5")!;
        var raw = (MessageCreateParams)result.RawRepresentationFactory!(null!)!;

        Assert.That(result.Reasoning, Is.Null, "adaptive thinking would be rejected by this model");
        Assert.That(options.Reasoning, Is.Not.Null, "the caller's options are not modified");
        Assert.That(raw.Model.ToString(), Does.Contain("claude-haiku-4-5"));
        Assert.That(raw.Thinking!.Value, Is.TypeOf<ThinkingConfigEnabled>());
        Assert.That(((ThinkingConfigEnabled)raw.Thinking.Value).BudgetTokens, Is.EqualTo(4_096).And.LessThan(raw.MaxTokens));
    }

    [Test]
    public void WithThinkingBudget_WithoutReasoning_LeavesOptionsAlone()
    {
        var options = new ChatOptions { MaxOutputTokens = 16_000 };
        Assert.That(LlmClientFactory.WithThinkingBudget(options, "claude-haiku-4-5"), Is.SameAs(options));
    }
}
