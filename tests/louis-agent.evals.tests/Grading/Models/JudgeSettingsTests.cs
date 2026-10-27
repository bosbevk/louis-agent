using LouisAgent.Core.Llm;
using LouisAgent.Core.Config;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Evals.Grading.Models;
using Microsoft.Extensions.AI;
using LouisAgent.Choices;

namespace LouisAgent.Evals.Tests.Grading.Models;

// F18 step 6.1: the judge is chosen by two settings, EVAL_JUDGE_PROVIDER and EVAL_JUDGE_MODEL (and EVAL_JUDGE_ENDPOINT for a local server), read once with
// SettingChoice (ADR 0024). With none set there is no judge, which a run survives. Written first: JudgeSettings and JudgeClients did not exist.
public class JudgeSettingsTests
{
    private static JudgeSettings? Read(params (string Name, string Value)[] env) =>
        JudgeSettings.FromEnvironment(name => env.Where(e => e.Name == name).Select(e => e.Value).FirstOrDefault());

    // F18 step 6.1b: a record prints all its members, and a key in a log line is a leaked key.
    [Test]
    public void TheSettingsText_NeverPrintsTheKey()
    {
        var settings = Read(("EVAL_JUDGE_MODEL", "claude-haiku-5-5"), ("EVAL_JUDGE_API_KEY", "sk-secret-123"))!;

        Assert.That(settings.ApiKey, Is.EqualTo("sk-secret-123"));
        Assert.That(settings.ToString(), Does.Not.Contain("sk-secret-123").And.Contain("claude-haiku-5-5").And.Contain("set"));
    }

    [Test]
    public void WithNoSettings_ThereIsNoJudge()
    {
        Assert.That(Read(), Is.Null);
        Assert.That(Read(("EVAL_JUDGE_PROVIDER", "ollama")), Is.Null, "a provider with no model names nothing to ask");
    }

    [Test]
    public void ProviderAndModel_AreRead_WithTheCaseIgnored_AndAnEndpointWhenGiven()
    {
        var settings = Read(("EVAL_JUDGE_PROVIDER", "Ollama"), ("EVAL_JUDGE_MODEL", "qwen3:8b"), ("EVAL_JUDGE_ENDPOINT", "http://ollama:11434"))!;

        Assert.That(settings, Is.EqualTo(new JudgeSettings(JudgeProvider.Ollama, "qwen3:8b", "http://ollama:11434", null)));
    }

    [Test]
    public void ThePaidProviderIsAChoice_ButOnlyEverOnPurpose()
    {
        Assert.That(Read(("EVAL_JUDGE_PROVIDER", "anthropic"), ("EVAL_JUDGE_MODEL", "claude-haiku-5-5"))!.Provider, Is.EqualTo(JudgeProvider.Anthropic));
        Assert.That(Read(("EVAL_JUDGE_PROVIDER", "openai-compatible"), ("EVAL_JUDGE_MODEL", "m"))!.Provider, Is.EqualTo(JudgeProvider.OpenAiCompatible), "the provider's own spelling");
    }

    [Test]
    public void WithNoProvider_AModelPicksTheFreeLocalOne_ExceptAClaude()
    {
        Assert.That(Read(("EVAL_JUDGE_MODEL", "qwen3:8b"))!.Provider, Is.EqualTo(JudgeProvider.Ollama), "a judge that nobody asked to pay for is local");
        Assert.That(Read(("EVAL_JUDGE_MODEL", "claude-haiku-5-5"))!.Provider, Is.EqualTo(JudgeProvider.Anthropic));
    }

    [Test]
    public void AnUnknownProvider_IsRefusedWithTheSupportedOnes_NeverAGuessedJudge()
    {
        var error = Assert.Throws<InvalidDataException>(() => Read(("EVAL_JUDGE_PROVIDER", "gpt"), ("EVAL_JUDGE_MODEL", "m")))!;

        Assert.That(error.Message, Does.Contain("EVAL_JUDGE_PROVIDER").And.Contain("gpt").And.Contain("ollama").And.Contain("anthropic"));
    }

    [Test]
    public async Task TheJudgesClient_IsBuiltThroughTheFactory_AndItsRequestsAreRecordedAsGradingWithTheRunsTag()
    {
        var sink = new ListUsageSink();
        var factory = new FakeFactory();
        var settings = new JudgeSettings(JudgeProvider.Ollama, "qwen3:8b", "http://ollama:11434", null);

        var judge = JudgeClients.Create(settings, factory, sink);

        Assert.That(judge.Model, Is.EqualTo("qwen3:8b"));
        Assert.That((factory.Asked!.Provider, factory.Asked.Model, factory.Asked.Endpoint), Is.EqualTo((LlmOptions.Ollama, "qwen3:8b", "http://ollama:11434")));
        using (UsageScope.Begin(UsagePurpose.Grading, tags: new UsageTags("bobotie", "run_1", null)))
            await judge.Chat.ChatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "is this a meal?")]);
        var row = sink.Records.Single();
        Assert.That((row.Purpose, row.Run, row.Model), Is.EqualTo((UsagePurpose.Grading, "run_1", "qwen3:8b")));
    }

    [Test]
    public async Task WithNoLedger_TheJudgeStillAsks_AndNothingIsRecorded()
    {
        var judge = JudgeClients.Create(new JudgeSettings(JudgeProvider.Ollama, "qwen3:8b", null, null), new FakeFactory(), sink: null);

        var response = await judge.Chat.ChatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "is this a meal?")]);

        Assert.That(response.Text, Is.EqualTo("yes"));
    }

    // F18 step 6.2: the judge a run uses is built from the environment, and only when a model is named.
    [Test]
    public void FromTheEnvironment_NoModelIsNoJudge_AndTheFactoryIsNeverAsked()
    {
        var factory = new FakeFactory();

        var judge = JudgeClients.FromEnvironment(_ => null, factory, () => throw new InvalidOperationException("no ledger is opened for no judge"));

        Assert.That(judge, Is.Null);
        Assert.That(factory.Asked, Is.Null);
    }

    [Test]
    public void FromTheEnvironment_AModelIsAJudge_RecordedInTheLedgerTheCallerOpens()
    {
        var sink = new ListUsageSink();
        var judge = JudgeClients.FromEnvironment(name => name == "EVAL_JUDGE_MODEL" ? "qwen3:8b" : null, new FakeFactory(), () => sink);

        Assert.That(judge!.Model, Is.EqualTo("qwen3:8b"));
        judge.Chat.ChatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "x")]).GetAwaiter().GetResult();
        Assert.That(sink.Records, Has.Count.EqualTo(1));
    }

    private sealed class FakeFactory : ILlmClientFactory
    {
        public LlmOptions? Asked { get; private set; }

        public IChatClient Create(LlmOptions options)
        {
            Asked = options;
            return new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "yes")));
        }
    }
}
