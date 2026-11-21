using LouisAgent.Core.Config;
using LouisAgent.Core.Llm;
using LouisAgent.Evals.Grading.Models;
using Microsoft.Extensions.AI;

namespace LouisAgent.Evals.Tests.Grading.Models;

/// <summary>
/// The judges ask for temperature 0, so the same answer gets the same verdict. A local model takes that, but Anthropic
/// refuses it outright ("`temperature` is deprecated for this model"), which left every paid judge unable to run.
/// </summary>
public class JudgeTemperatureTests
{
    /// <summary>What a judge asks for: temperature 0, and a limit that must survive.</summary>
    private static readonly ChatOptions Asked = new() { Temperature = 0, MaxOutputTokens = 500 };

    /// <summary>The options that reach the provider when a judge of this provider asks.</summary>
    private static async Task<ChatOptions?> ReachingTheProvider(JudgeProvider provider)
    {
        var factory = new CapturingFactory();
        var judge = JudgeClients.Create(new JudgeSettings(provider, "a-model", null, null), factory, sink: null);

        await judge.Chat.ChatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "is this the page's?")], Asked);

        return factory.Client.Options;
    }

    /// <summary>Anthropic gets no temperature, and the rest of what was asked still reaches it.</summary>
    [Test]
    public async Task APaidJudge_SendsNoTemperature_AndKeepsTheRest()
    {
        var options = await ReachingTheProvider(JudgeProvider.Anthropic);

        Assert.That(options!.Temperature, Is.Null);
        Assert.That(options.MaxOutputTokens, Is.EqualTo(500));
    }

    /// <summary>The local judge keeps it: that is what makes a re-score repeat exactly.</summary>
    [TestCase(JudgeProvider.Ollama)]
    [TestCase(JudgeProvider.OpenAiCompatible)]
    public async Task AnotherJudge_KeepsTheTemperature(JudgeProvider provider) =>
        Assert.That((await ReachingTheProvider(provider))!.Temperature, Is.EqualTo(0));

    /// <summary>The caller's options are its own: the next judge to use them still asks for temperature 0.</summary>
    [Test]
    public async Task TheCallersOptions_AreLeftAlone()
    {
        await ReachingTheProvider(JudgeProvider.Anthropic);

        Assert.That(Asked.Temperature, Is.EqualTo(0));
    }

    /// <summary>A factory whose one client remembers the options it was sent.</summary>
    private sealed class CapturingFactory : ILlmClientFactory
    {
        public CapturingClient Client { get; } = new();

        /// <inheritdoc />
        public IChatClient Create(LlmOptions options) => Client;
    }

    /// <summary>A model that says yes and remembers what it was sent.</summary>
    private sealed class CapturingClient : IChatClient
    {
        public ChatOptions? Options { get; private set; }

        /// <inheritdoc />
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "yes")));
        }

        /// <inheritdoc />
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
