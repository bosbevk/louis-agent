using LouisAgent.Core.Llm.Thinking;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Llm.Thinking;

/// <summary>
/// The default way a resumed chat handles thinking that no longer matches its tools: the thinking goes, and a message
/// that held nothing else goes with it, so no empty message is sent.
/// </summary>
public class StripStaleThinkingTests
{
    /// <summary>A message of thinking alone is dropped; the messages around it stay as they were.</summary>
    [Test]
    public void AMessageOfThinkingAlone_IsDropped()
    {
        ChatMessage[] saved =
        [
            new(ChatRole.User, "Plan a dinner"),
            new(ChatRole.Assistant, [new TextReasoningContent("Fish is quick.")]),
            new(ChatRole.Assistant, "Fish.")
        ];

        var restored = StripStaleThinking.Instance.Restore(saved);

        Assert.That(restored.Select(message => message.Text), Is.EqualTo(new[] { "Plan a dinner", "Fish." }));
    }
}
