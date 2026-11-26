using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Testing;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.EngineTools.Skills;

/// <summary>
/// F97.1 step 10b: every chat has the one door, <c>UseSkill</c>, from its first turn; its description lists the
/// approved skills, and no skill is a tool of its own, so the tools a chat sends don't grow with its skills. Inside a
/// turn the door knows the chat, so a chat reaches its own drafts and never another's.
/// </summary>
public class UseSkillTests
{
    private string _skills = "";

    /// <summary>A skills folder of its own for each test, with one approved skill.</summary>
    [SetUp]
    public void MakeTheSkillsFolder()
    {
        _skills = Directory.CreateTempSubdirectory("use-skill-").FullName;
        Skill("word-count");
    }

    /// <summary>The folder goes with the test.</summary>
    [TearDown]
    public void RemoveTheSkillsFolder() => Directory.Delete(_skills, recursive: true);

    /// <summary>
    /// The door is in the chat's tools, listing the approved skill; the skill isn't a tool of its own.
    /// </summary>
    [Test]
    public async Task AChat_SendsTheDoor_ListingTheApprovedSkills()
    {
        var model = new FakeChatClient(_ => Say("hello"));
        var engine = Engine(model);

        await engine.ProcessPromptAsync(engine.StartConversation(), "hi");

        Assert.Multiple(() =>
        {
            Assert.That(model.ToolNamesSent[0], Does.Contain("UseSkill").And.Not.Contain("word-count"));
            Assert.That(model.ToolDescriptionsSent[0]["UseSkill"], Does.Contain("- word-count: Counts words."));
        });
    }

    /// <summary>
    /// F97.1 step 13: a skill dropped in by hand is found after a reload, listed in the next chat, and an open chat's
    /// door stays as it was sent.
    /// </summary>
    [Test]
    public async Task AReload_ListsADroppedInSkill_InTheNextChat_AndLeavesTheOpenOne()
    {
        var model = new FakeChatClient(_ => Say("ok"), _ => Say("ok"), _ => Say("ok"));
        var engine = Engine(model);
        var open = engine.StartConversation();
        await engine.ProcessPromptAsync(open, "hi");

        Skill("line-count");
        engine.ReloadSkills();
        await engine.ProcessPromptAsync(open, "again");
        await engine.ProcessPromptAsync(engine.StartConversation(), "hi");

        Assert.Multiple(() =>
        {
            Assert.That(model.ToolDescriptionsSent[1]["UseSkill"], Does.Not.Contain("line-count"));
            Assert.That(model.ToolDescriptionsSent[2]["UseSkill"], Does.Contain("- line-count: Counts words."));
        });
    }

    /// <summary>The model's own <c>ReloadSkills</c> reads the skills folder again too.</summary>
    [Test]
    public async Task TheModelsReload_ReadsTheSkillsFolderAgain()
    {
        var reload = new FunctionCallContent("c1", "ReloadSkills", new Dictionary<string, object?>());
        var model = new FakeChatClient(
            _ => Say("ok"),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, [reload])),
            _ => Say("reloaded"),
            _ => Say("ok"));
        var engine = Engine(model);

        // A first chat reads the skills folder; the skill is dropped in after, so only a reload finds it.
        await engine.ProcessPromptAsync(engine.StartConversation(), "hi");
        Skill("line-count");
        await engine.ProcessPromptAsync(engine.StartConversation(), "reload");
        await engine.ProcessPromptAsync(engine.StartConversation(), "hi");

        Assert.That(model.ToolDescriptionsSent[3]["UseSkill"], Does.Contain("- line-count: Counts words."));
    }

    /// <summary>The model reads a skill through the door by its name alone.</summary>
    [Test]
    public async Task TheDoor_ReadsASkill_ByItsName() =>
        Assert.That(await Read("word-count", engine => engine.StartConversation()), Does.Contain("Give it a text."));

    /// <summary>A chat reaches its own draft through the door; another chat doesn't.</summary>
    [Test]
    public async Task AChat_ReachesItsOwnDraft_AndNoOtherChatDoes()
    {
        Skill(Path.Combine("drafts", "line-count"));

        string mine = await Read("line-count", engine =>
        {
            var chat = engine.StartConversation();
            chat.Drafted("line-count");
            return chat;
        });
        string theirs = await Read("line-count", engine => engine.StartConversation());

        Assert.Multiple(() =>
        {
            Assert.That(mine, Does.Contain("Give it a text."));
            Assert.That(theirs, Does.Contain("No skill this chat may use is called line-count"));
        });
    }

    /// <summary>What the door answered when the model, in this chat, asked to read this skill.</summary>
    private async Task<string> Read(string name, Func<AgentEngine, Conversation> chatOf)
    {
        var call = new FunctionCallContent("c1", "UseSkill", new Dictionary<string, object?> { ["name"] = name });
        var model = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, [call])),
            messages => Say(messages.Last().Contents.OfType<FunctionResultContent>().Single().Result!.ToString()!));
        var engine = Engine(model);

        return await engine.ProcessPromptAsync(chatOf(engine), "read it");
    }

    /// <summary>An engine on this test's skills folder.</summary>
    private AgentEngine Engine(IChatClient model)
    {
        var options = new AgentOptions { WorkspaceRoot = _skills, SkillsDirectory = _skills };
        return AgentEngine.CreateBuilder(SkillLoader.Load(options), model, options)
            .WithSkillsDirectory(_skills)
            .Build();
    }

    /// <summary>The model's answer in plain text.</summary>
    private static ChatResponse Say(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    /// <summary>A good skill's folder.</summary>
    private void Skill(string folder)
    {
        string path = Path.Combine(_skills, folder);
        Directory.CreateDirectory(Path.Combine(path, "scripts"));
        string skillFile = $"---\nname: {Path.GetFileName(folder)}\ndescription: Counts words.\n" +
                           "metadata:\n  script: scripts/run.py\n---\nGive it a text.\n";
        File.WriteAllText(Path.Combine(path, "SKILL.md"), skillFile);
        File.WriteAllText(Path.Combine(path, "scripts", "run.py"), "print(2)");
        File.WriteAllText(Path.Combine(path, "parameters.schema.json"),
            """{ "type": "object", "properties": { "text": { "type": "string" } } }""");
    }
}
