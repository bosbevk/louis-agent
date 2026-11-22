using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm;
using Microsoft.Extensions.AI;

namespace LouisAgent.Evals.IntegrationTests.Grading;

/// <summary>
/// The proof the judge stands on (F126.2 step 3): a Claude model, built as a host builds it, answers as a typed
/// record when asked with <c>GetResponseAsync&lt;T&gt;</c>, and the record says yes for an answer that meets a
/// criterion and no for one that doesn't. Two live calls on a small model, with no temperature set, since the newest
/// models refuse one.
/// </summary>
/// <remarks>
/// There is no code behind it to see fail first: it proves the provider, not ours. If it fails, the plan stops there.
/// </remarks>
[Category("Integration")]
public class StructuredAnswerProofTests
{
    /// <summary>The small model the proof runs on.</summary>
    private const string Model = "claude-haiku-5-5";

    /// <summary>The criterion both answers are marked against.</summary>
    private const string Criterion = "The answer names the capital of France.";

    /// <summary>A small answer of the proof's own: the reason first, then whether the criterion is met.</summary>
    private sealed record ProofAnswer(string Reason, bool Met);

    /// <summary>A plain answer that meets the criterion comes back met, with a reason.</summary>
    [Test]
    public async Task AnAnswerThatMeetsTheCriterion_ComesBack_Met() =>
        await AssertMarked("The capital of France is Paris.", true);

    /// <summary>A plain answer that misses it comes back not met, with a reason.</summary>
    [Test]
    public async Task AnAnswerThatMissesTheCriterion_ComesBack_NotMet() =>
        await AssertMarked("France is a country in western Europe with many old towns.", false);

    /// <summary>Asks the model to mark one answer, and checks the record it gives back.</summary>
    private static async Task AssertMarked(string answer, bool met)
    {
        string key = Outside.Key("ANTHROPIC_API_KEY", "the judge's model");
        var options = new LlmOptions { Provider = LlmOptions.Anthropic, Model = Model, ApiKey = key };
        var model = new LlmClientFactory([AnthropicProvider.Instance]).Create(options);

        string ask = $"Criterion: {Criterion}\nAnswer: {answer}\nSay whether the answer meets the criterion, and why.";
        var response = await model.GetResponseAsync<ProofAnswer>(ask);

        var usage = response.Usage;
        TestContext.Out.WriteLine($"tokens in {usage?.InputTokenCount}, out {usage?.OutputTokenCount}");
        Assert.Multiple(() =>
        {
            Assert.That(response.Result.Met, Is.EqualTo(met), response.Result.Reason);
            Assert.That(response.Result.Reason, Is.Not.Empty);
        });
    }
}
