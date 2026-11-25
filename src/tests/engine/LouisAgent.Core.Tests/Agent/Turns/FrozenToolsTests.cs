using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.Turns;

/// <summary>
/// A conversation keeps the tools it started with: whatever changes the engine's tools mid-chat reaches the next
/// chat, never the one open, so the model's earlier thinking still matches what it was sent.
/// </summary>
public class FrozenToolsTests
{
    /// <summary>A tool the agent could make: its definition, as CreateTool takes it.</summary>
    private const string AddTool = """
                                   # Tool: AddNumbers
                                   - Description: Add two integers
                                   - Parameters:
                                     - `a` (integer, required): first number
                                   - Implementation:

                                   ```python
                                   print(1)
                                   ```
                                   """;

    private string _skillsDir = null!;

    /// <summary>Each test gets an empty skills folder of its own, where made tools wait.</summary>
    [SetUp]
    public void SetUp() => _skillsDir = Directory.CreateTempSubdirectory("frozen-tools-").FullName;

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    /// <summary>An engine on the test's folder that can make tools, on the model given.</summary>
    private AgentEngine Engine(IChatClient client)
    {
        var options = new AgentOptions
        {
            WorkspaceRoot = _skillsDir,
            SkillsDirectory = _skillsDir
        };
        return new AgentEngine(SkillLoader.Load(options), client, options) { SkillsDirectory = _skillsDir };
    }

    /// <summary>The model's answer in plain text.</summary>
    private static ChatResponse Say(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    /// <summary>The names of the tools a request was sent with, as they were when it went.</summary>
    private static IReadOnlyList<string> ToolNames(FakeChatClient client, int request) =>
        client.ToolNamesSent[request];

    /// <summary>
    /// Making a tool tells the model it reaches the next chat, so it doesn't promise to use it in this one.
    /// </summary>
    [Test]
    public void CreateTool_SaysTheToolReachesTheNextChat()
    {
        var engine = Engine(new FakeChatClient());

        string reply = engine.CreateTool(AddTool);

        Assert.That(reply, Does.Contain("next chat").And.Not.Contain("call it now"));
    }

    /// <summary>Approving a tool tells the user it reaches their next chat.</summary>
    [Test]
    public void ApproveATool_SaysItReachesTheNextChat()
    {
        var engine = Engine(new FakeChatClient());
        engine.CreateTool(AddTool);

        string reply = engine.HandleUserCommand("/approve AddNumbers")!;

        Assert.That(reply, Does.Contain("Approved").And.Contain("next chat"));
    }

    /// <summary>A tool the model makes mid-message isn't sent on that message's later requests.</summary>
    [Test]
    public async Task AToolMadeMidMessage_LeavesTheMessagesLaterRequestsAlone()
    {
        var makeIt = new FunctionCallContent("c1", nameof(AgentEngine.CreateTool),
            new Dictionary<string, object?> { ["definition"] = AddTool });
        var client = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, [makeIt])),
            _ => Say("made it"));
        var engine = Engine(client);

        await engine.ProcessPromptAsync(engine.StartConversation(), "make a tool");

        Assert.That(engine.Tools.Select(t => t.Name), Does.Contain("AddNumbers"), "the engine has it");
        Assert.That(ToolNames(client, 1), Is.EqualTo(ToolNames(client, 0)), "the message's next request doesn't");
    }

    /// <summary>A tool rejected between two messages stays in the open conversation's list.</summary>
    [Test]
    public async Task AToolRejectedBetweenMessages_StaysInTheOpenConversation()
    {
        var client = new FakeChatClient(_ => Say("one"), _ => Say("two"));
        var engine = Engine(client);
        engine.CreateTool(AddTool);
        var conversation = engine.StartConversation();

        await engine.ProcessPromptAsync(conversation, "first");
        engine.HandleUserCommand("/reject AddNumbers");
        await engine.ProcessPromptAsync(conversation, "second");

        Assert.That(engine.Tools.Select(t => t.Name), Does.Not.Contain("AddNumbers"), "the engine dropped it");
        Assert.That(ToolNames(client, 1), Is.EqualTo(ToolNames(client, 0)), "the open conversation didn't");
    }

    /// <summary>A tool approved mid-chat keeps, in the open chat, the description it was first sent with.</summary>
    [Test]
    public async Task AToolApprovedBetweenMessages_KeepsTheDescriptionItWasSentWith()
    {
        var client = new FakeChatClient(_ => Say("one"), _ => Say("two"));
        var engine = Engine(client);
        engine.CreateTool(AddTool);
        var conversation = engine.StartConversation();

        await engine.ProcessPromptAsync(conversation, "first");
        engine.HandleUserCommand("/approve AddNumbers");
        await engine.ProcessPromptAsync(conversation, "second");

        Assert.That(client.ToolDescriptionsSent[1]["AddNumbers"],
            Is.EqualTo(client.ToolDescriptionsSent[0]["AddNumbers"]));
    }

    /// <summary>
    /// A tool rejected while a chat is open stays in its list, as it was sent, but never runs: the model is told.
    /// </summary>
    [Test]
    public async Task AToolRejectedBetweenMessages_NeverRuns()
    {
        var callIt = new FunctionCallContent("c1", "AddNumbers", new Dictionary<string, object?> { ["a"] = 1 });
        var client = new FakeChatClient(_ => Say("one"),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, [callIt])),
            messages => Say(messages.Last().Contents.OfType<FunctionResultContent>().Single().Result!.ToString()!));
        var engine = Engine(client);
        engine.CreateTool(AddTool);
        var conversation = engine.StartConversation();

        await engine.ProcessPromptAsync(conversation, "first");
        engine.HandleUserCommand("/reject AddNumbers");
        string answer = await engine.ProcessPromptAsync(conversation, "add 1");

        Assert.That(answer, Does.Contain("rejected").And.Contain("NOT run"));
    }

    /// <summary>
    /// A tool made while one conversation is open doesn't reach it, though the engine is shared; a new one has it.
    /// </summary>
    [Test]
    public async Task AToolMadeElsewhere_ReachesTheNextConversation_NotTheOpenOne()
    {
        var client = new FakeChatClient(_ => Say("one"), _ => Say("two"), _ => Say("three"));
        var engine = Engine(client);
        var open = engine.StartConversation();

        await engine.ProcessPromptAsync(open, "first");
        engine.CreateTool(AddTool);
        await engine.ProcessPromptAsync(open, "second");
        await engine.ProcessPromptAsync(engine.StartConversation(), "a new chat");

        Assert.That(ToolNames(client, 1), Does.Not.Contain("AddNumbers"), "the open conversation");
        Assert.That(ToolNames(client, 2), Does.Contain("AddNumbers"), "the next conversation");
    }
}
