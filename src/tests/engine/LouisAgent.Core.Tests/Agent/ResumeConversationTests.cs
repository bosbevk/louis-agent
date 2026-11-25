using LouisAgent.Core.Agent;
using LouisAgent.Core.Skills.Providers;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>
/// A saved chat brought back: on the tools it was saved with its turns come back whole, thinking included; on tools a
/// release moved since, the default strategy strips the thinking, which would no longer match, and keeps the rest.
/// </summary>
public class ResumeConversationTests
{
    /// <summary>An engine on the sample skills, offering its own tools.</summary>
    private static AgentEngine Engine() =>
        new(new MarkdownSkillProvider(TestPaths.SampleSkills), new FakeChatClient(), TestPaths.Agent());

    /// <summary>
    /// A saved turn: the question, the model's thinking, its words and a call, the result, the answer.
    /// </summary>
    private static ChatMessage[] SavedTurn() =>
    [
        new(ChatRole.User, "What time is it?"),
        new(ChatRole.Assistant,
        [
            new TextReasoningContent("The clock tool knows."), new TextContent("Let me look."),
            new FunctionCallContent("c1", "GetToday")
        ]),
        new(ChatRole.Tool, [new FunctionResultContent("c1", "Tuesday")]),
        new(ChatRole.Assistant, "It's Tuesday.")
    ];

    /// <summary>Every piece of the chat's messages after its opening, in order.</summary>
    private static List<AIContent> Restored(Conversation conversation) =>
        [.. conversation.Messages.Skip(1).SelectMany(message => message.Contents)];

    /// <summary>On tools moved since, the thinking goes and the words, the call and its result stay.</summary>
    [Test]
    public void OnToolsMovedSince_TheThinkingIsStripped_TheRestKept()
    {
        var conversation = Engine().ResumeConversation("You tell the time.", new ToolsFingerprint("older tools"),
            SavedTurn());

        var restored = Restored(conversation);

        Assert.Multiple(() =>
        {
            Assert.That(restored.OfType<TextReasoningContent>(), Is.Empty, "the thinking");
            Assert.That(restored.OfType<TextContent>().Select(text => text.Text),
                Is.EqualTo(new[] { "What time is it?", "Let me look.", "It's Tuesday." }), "the words");
            Assert.That(restored.OfType<FunctionCallContent>().Single().Name, Is.EqualTo("GetToday"), "the call");
            Assert.That(restored.OfType<FunctionResultContent>().Single().Result, Is.EqualTo("Tuesday"), "its result");
        });
    }

    /// <summary>On the tools it was saved with, the chat comes back whole, thinking and all.</summary>
    [Test]
    public void OnTheSameTools_TheTurnsComeBackWhole()
    {
        var engine = Engine();
        var today = ToolsFingerprint.Of(engine.Tools);

        var conversation = engine.ResumeConversation("You tell the time.", today, SavedTurn());

        Assert.That(Restored(conversation).OfType<TextReasoningContent>().Single().Text,
            Is.EqualTo("The clock tool knows."));
    }
}
