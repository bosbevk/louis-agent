using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Testing;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.Turns;

/// <summary>
/// A conversation keeps the tools it started with (FX4): whatever changes the engine's tools mid-chat reaches the next
/// chat, never the one open, so the model's earlier thinking still matches what it was sent. Since F97.1 a skill is
/// never a tool of its own, so what changes mid-chat is the door's list of approved skills; and a tool the engine no
/// longer offers is never run, though an open chat still lists it.
/// </summary>
public class FrozenToolsTests
{
    private string _skills = null!;

    /// <summary>Each test gets a skills folder of its own, with one draft waiting.</summary>
    [SetUp]
    public void SetUp()
    {
        _skills = Directory.CreateTempSubdirectory("frozen-tools-").FullName;
        string draft = Path.Combine(_skills, "drafts", "line-count");
        Directory.CreateDirectory(Path.Combine(draft, "scripts"));
        File.WriteAllText(Path.Combine(draft, "SKILL.md"),
            "---\nname: line-count\ndescription: Counts lines.\nmetadata:\n  script: scripts/run.py\n---\n# Count\n");
        File.WriteAllText(Path.Combine(draft, "scripts", "run.py"), "print(2)");
        File.WriteAllText(Path.Combine(draft, "parameters.schema.json"), """{ "type": "object", "properties": {} }""");
    }

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_skills, recursive: true);

    /// <summary>
    /// A skill approved while a chat is open leaves that chat's door as it was sent, and the next chat lists it.
    /// </summary>
    [Test]
    public async Task ASkillApprovedBetweenMessages_LeavesTheOpenChatsDoor_AndReachesTheNext()
    {
        var model = new FakeChatClient(_ => Say("one"), _ => Say("two"), _ => Say("three"));
        var engine = Engine(model);
        var open = engine.StartConversation();

        await engine.ProcessPromptAsync(open, "first");
        engine.HandleUserCommand("/approve line-count", chat: open);
        await engine.ProcessPromptAsync(open, "second");
        await engine.ProcessPromptAsync(engine.StartConversation(), "a new chat");

        Assert.Multiple(() =>
        {
            Assert.That(model.ToolDescriptionsSent[1]["UseSkill"],
                Is.EqualTo(model.ToolDescriptionsSent[0]["UseSkill"]));
            Assert.That(model.ToolNamesSent[1], Is.EqualTo(model.ToolNamesSent[0]));
            Assert.That(model.ToolDescriptionsSent[2]["UseSkill"], Does.Contain("- line-count: Counts lines."));
        });
    }

    /// <summary>
    /// A tool the engine no longer offers is never run, though an open chat still lists it: the model is told.
    /// </summary>
    [Test]
    public async Task AToolNoLongerOffered_NeverRuns()
    {
        bool ran = false;
        var tool = AIFunctionFactory.Create(() => ran = true, "AddNumbers", "Adds two numbers.");
        var frozen = new FrozenTool(tool, _ => false);

        object? answer = await frozen.InvokeAsync(new AIFunctionArguments());

        Assert.Multiple(() =>
        {
            Assert.That(ran, Is.False);
            Assert.That(answer?.ToString(), Does.Contain("NOT run"));
        });
    }

    /// <summary>An engine on the test's skills folder, on the model given.</summary>
    private AgentEngine Engine(IChatClient model)
    {
        var options = new AgentOptions { WorkspaceRoot = _skills, SkillsDirectory = _skills };
        return AgentEngine.CreateBuilder(SkillLoader.Load(options), model, options)
            .WithSkillsDirectory(_skills)
            .Build();
    }

    /// <summary>The model's answer in plain text.</summary>
    private static ChatResponse Say(string text) => new(new ChatMessage(ChatRole.Assistant, text));
}
