using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Grading;

namespace LouisAgent.Evals.IntegrationTests.Grading;

/// <summary>
/// The real judge on a real model (F126.2 step 11): it marks two criteria of the guide's first turn on
/// <c>claude-haiku-5-5</c>, one the answer plainly meets and one it plainly doesn't. It skips without a key.
/// </summary>
[Category("Integration")]
public class JudgeLiveTests
{
    /// <summary>The small model the judge runs on here.</summary>
    private const string Model = "claude-haiku-5-5";

    /// <summary>The guide's first turn, as it might have gone.</summary>
    private static readonly TurnTranscript Turn = new(
        "What is louis-agent, in one minute?",
        "louis-agent is an AI agent framework in C#. I read it in docs/README.md. Want to see the architecture page?",
        [
            new ToolCallTranscript("ReadDoc", """{"path":"docs/README.md"}""",
                "# louis-agent\nAn AI agent framework in C#.")
        ],
        TimeSpan.FromSeconds(11));

    /// <summary>A criterion the answer plainly meets comes back met; one it plainly misses, not met.</summary>
    [TestCase("Names the doc it read", true)]
    [TestCase("Gives the price of a licence in rand", false)]
    public async Task TheJudge_MarksACriterion_OnARealModel(string criterion, bool met)
    {
        string key = Outside.Key("ANTHROPIC_API_KEY", "the judge's model");
        var options = new LlmOptions
        {
            Provider = LlmOptions.Anthropic,
            Model = Model,
            ApiKey = key
        };
        var judge = new Judge(new LlmClientFactory([AnthropicProvider.Instance]).Create(options));

        var mark = await judge.MarkAsync(criterion, [Turn]);

        Assert.That(mark.Met, Is.EqualTo(met), mark.Reason);
    }
}
