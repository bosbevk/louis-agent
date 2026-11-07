using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic.Tests;

/// <summary>Older Claude models take a fixed thinking budget in place of adaptive thinking; the swap, and who gets it.</summary>
public class ThinkingBudgetTests
{
    [TestCase("claude-haiku-4-5-20251001", true)]
    [TestCase("claude-haiku-5-5", false)]
    [TestCase("claude-sonnet-4-5", true)]
    [TestCase("claude-opus-4-6", false)]
    [TestCase("claude-opus-5-5", false)]
    public void UsesThinkingBudget_OnlyForModelsBeforeAdaptiveThinking(string model, bool expected) =>
        Assert.That(ThinkingBudget.UsesThinkingBudget(model), Is.EqualTo(expected));

    [Test]
    public void WithThinkingBudget_ReplacesReasoningWithABudgetBelowMaxTokens()
    {
        var options = new ChatOptions
        {
            MaxOutputTokens = 16_000,
            Reasoning = new ReasoningOptions
            {
                Effort = ReasoningEffort.Medium,
                Output = ReasoningOutput.Full
            },
        };

        var result = ThinkingBudget.WithThinkingBudget(options, "claude-haiku-4-5")!;
        var raw = (MessageCreateParams)result.RawRepresentationFactory!(null!)!;

        Assert.That(result.Reasoning, Is.Null, "adaptive thinking would be rejected by this model");
        Assert.That(options.Reasoning, Is.Not.Null, "the caller's options are not modified");
        Assert.That(raw.Model.ToString(), Does.Contain("claude-haiku-4-5"));
        Assert.That(raw.Thinking!.Value, Is.TypeOf<ThinkingConfigEnabled>());
        Assert.That(((ThinkingConfigEnabled)raw.Thinking.Value).BudgetTokens,
            Is.EqualTo(4_096).And.LessThan(raw.MaxTokens));
    }

    [Test]
    public void WithThinkingBudget_WithoutReasoning_LeavesOptionsAlone()
    {
        var options = new ChatOptions { MaxOutputTokens = 16_000 };

        Assert.That(ThinkingBudget.WithThinkingBudget(options, "claude-haiku-4-5"), Is.SameAs(options));
    }
}
