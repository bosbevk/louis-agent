using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Testing;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.EngineTools;

/// <summary>
/// The prompt's skill files read again: a reload reaches the next chat's system prompt and leaves the open chat's as
/// it was sent (FX4), and only the function files in their own folder are read.
/// </summary>
public class SkillReloadTests
{
    private const string EchoSkill = """
                                     # Greeting Skills

                                     Greets people.

                                     ## Skill: Greet

                                     - Description: Print a greeting
                                     - Execution:

                                     ```bash
                                     echo "hello"
                                     ```
                                     """;

    private string _skillsDir = null!;

    /// <summary>Each test gets an empty skills folder of its own.</summary>
    [SetUp]
    public void SetUp() => _skillsDir = Directory.CreateTempSubdirectory("skills-").FullName;

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    /// <summary>A function skill left at the folder's root, the old place, isn't loaded.</summary>
    [Test]
    public void AFunctionFileLeftAtTheRoot_IsntRead()
    {
        File.WriteAllText(Path.Combine(_skillsDir, "farewell-skills.md"), EchoSkill.Replace("Greet", "Farewell"));

        Assert.That(SkillLoader.Load(Options()).Skills.Select(s => s.Name), Does.Not.Contain("Farewell"));
    }

    /// <summary>A function skill in its folder loads with the skills toolset, with no setting to turn it on.</summary>
    [Test]
    public void AFunctionFile_LoadsWithTheSkillsToolset_NoSettingNeeded()
    {
        File.WriteAllText(FunctionFile("greeting"), EchoSkill);

        Assert.That(SkillLoader.Load(Options()).Skills.Select(s => s.Name), Does.Contain("Greet"));
    }

    /// <summary>
    /// A skill file reloaded mid-chat leaves the open chat's system prompt as it was sent; the next chat gets the new
    /// one.
    /// </summary>
    [Test]
    public async Task AReload_KeepsTheOpenChatsSystemPrompt_TheNextChatGetsTheNewOne()
    {
        var client = new FakeChatClient(_ => Say("first"), _ => Say("second"), _ => Say("third"));
        var engine = Engine(client);
        var open = engine.StartConversation();

        await engine.ProcessPromptAsync(open, "before");
        File.WriteAllText(FunctionFile("greeting"), EchoSkill);
        engine.ReloadSkills();
        await engine.ProcessPromptAsync(open, "after");
        await engine.ProcessPromptAsync(engine.StartConversation(), "a new chat");

        Assert.That(client.Requests[1].Messages[0].Text, Is.EqualTo(client.Requests[0].Messages[0].Text),
            "the open chat's prompt is as it was sent");
        Assert.That(client.Requests[2].Messages[0].Text, Does.Contain("# Greeting Skills"), "the next chat's has it");
    }

    /// <summary>Settings that point the workspace and the skills at the test's folder.</summary>
    private AgentOptions Options() => new() { WorkspaceRoot = _skillsDir, SkillsDirectory = _skillsDir };

    /// <summary>A function skill file's place in the test's folder, made ready to be written.</summary>
    private string FunctionFile(string name)
    {
        string folder = Path.Combine(_skillsDir, PromptFolder.Agents.Wire(), PromptFolder.Functions.Wire());
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, $"{name}.md");
    }

    /// <summary>An engine on the test's folder that can reload its skills.</summary>
    private AgentEngine Engine(IChatClient client)
    {
        var options = Options();
        return new AgentEngine(SkillLoader.Load(options), client, options)
        {
            SkillsDirectory = _skillsDir,
            SkillReloader = () => SkillLoader.Load(options)
        };
    }

    /// <summary>The model's answer in plain text.</summary>
    private static ChatResponse Say(string text) => new(new ChatMessage(ChatRole.Assistant, text));
}
