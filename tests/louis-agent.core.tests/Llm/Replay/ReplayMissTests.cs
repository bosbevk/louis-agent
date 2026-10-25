using LouisAgent.Core.Llm.Replay;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Llm.Replay;

// F18 step 4e.2: a replay's miss never calls the model, and says which part of the request changed (ADR 0005: a failure carries its
// reason). Pure functions over two asks, so no store is needed here.
public class ReplayMissTests
{
    private static AIFunction Tool(string name, string description = "does it") => AIFunctionFactory.Create(() => "x", name, description);

    private static ModelAsk Ask(
        string system = "You are Puls.", string[]? messages = null, ChatOptions? options = null, string model = "m1")
    {
        options ??= new ChatOptions { Tools = [Tool("Search"), Tool("Fetch")] };
        options.ModelId = model;
        var chat = new List<ChatMessage> { new(ChatRole.System, system) };
        foreach (string text in messages ?? ["one", "two"])
            chat.Add(new ChatMessage(chat.Count % 2 == 1 ? ChatRole.User : ChatRole.Assistant, text));
        return ModelAsk.Of(chat, options, null);
    }

    private static string Miss(ModelAsk requested, params ModelAsk[] recorded) => ReplayMiss.Describe("r1:a:1/t1", requested, recorded);

    [Test]
    public void TheModel()
    {
        Assert.That(Miss(Ask(model: "m2"), Ask()), Does.Contain("r1:a:1/t1").And.Contain("model").And.Contain("m1").And.Contain("m2"));
    }

    [Test]
    public void TheSystemPrompt()
    {
        Assert.That(Miss(Ask(system: "You are Louis."), Ask()), Does.Contain("system prompt"));
    }

    [Test]
    public void AToolThatChanged_AddedOrGone_IsNamed()
    {
        Assert.That(Miss(Ask(options: new ChatOptions { Tools = [Tool("Search", "other"), Tool("Fetch")] }), Ask()), Does.Contain("tool 'Search' changed"));
        Assert.That(Miss(Ask(options: new ChatOptions { Tools = [Tool("Search"), Tool("Fetch"), Tool("Extra")] }), Ask()), Does.Contain("tool 'Extra' was added"));
        Assert.That(Miss(Ask(options: new ChatOptions { Tools = [Tool("Search")] }), Ask()), Does.Contain("tool 'Fetch' is gone"));
    }

    [Test]
    public void TheSettings()
    {
        Assert.That(Miss(Ask(options: new ChatOptions { Tools = [Tool("Search"), Tool("Fetch")], Temperature = 0.9f }), Ask()), Does.Contain("settings"));
    }

    [Test]
    public void AMessage_IsCountedFromOne_AfterTheSystemPrompt_WithItsRole()
    {
        Assert.That(Miss(Ask(messages: ["one", "TWO"]), Ask()), Does.Contain("message 2 (assistant) changed"));
        Assert.That(Miss(Ask(messages: ["one", "two", "three"]), Ask()), Does.Contain("message 3 (user) was added"));
        Assert.That(Miss(Ask(messages: ["one"]), Ask()), Does.Contain("message 2 (assistant) is gone"));
    }

    [Test]
    public void ATrialWithNoModelAnswers_SaysItWasRecordedBeforeOrIsAnotherRun()
    {
        string text = Miss(Ask());

        Assert.That(text, Does.Contain("r1:a:1/t1").And.Contain("recorded before step 4e").And.Contain("different run"));
    }

    [Test]
    public void TheLongestMatchingRecording_IsTheOneCompared()
    {
        // One recording differs from the request in the system prompt; the other only in message 2. The second matches longer.
        ModelAsk farther = Ask(system: "You are Louis.");
        ModelAsk nearer = Ask(messages: ["one", "OTHER"]);

        string text = Miss(Ask(), farther, nearer);

        Assert.That(text, Does.Contain("message 2").And.Not.Contain("system prompt"));
    }
}
