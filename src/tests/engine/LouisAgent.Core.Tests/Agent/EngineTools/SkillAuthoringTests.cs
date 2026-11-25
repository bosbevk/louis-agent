using LouisAgent.Composition.Prompts;
using LouisAgent.Testing;
using LouisAgent.Choices;
using LouisAgent.Core.Skills.Languages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;
using LouisAgent.Composition.Tools.Scripts;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Core.Tests.Agent.EngineTools;

/// <summary>
/// The agent writing its own skill files: they wait in pending/ for the user's /approve, a bad or reserved name is
/// refused, and a reload picks up what is on disk.
/// </summary>
public class SkillAuthoringTests
{
    private const string EchoSkill = """
                                     # Greeting Skills

                                     Greets people.

                                     ## Skill: Greet

                                     - Description: Print a greeting
                                     - Parameters:
                                       - `who` (required): who to greet
                                     - Execution:

                                     ```bash
                                     echo "hello ${SKILL_ARG_who}"
                                     ```
                                     """;

    private string _skillsDir = null!;

    /// <summary>Each test gets an empty skills folder of its own.</summary>
    [SetUp]
    public void SetUp()
    {
        _skillsDir = Directory.CreateTempSubdirectory("skills-").FullName;
    }

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    /// <summary>A function skill file (CL8: agent/functions/{name}.md), as a user would drop one in.</summary>
    private string FunctionFile(string name)
    {
        Directory.CreateDirectory(Path.Combine(_skillsDir, PromptFolder.Agents.Wire(), PromptFolder.Functions.Wire()));
        return Path.Combine(_skillsDir, PromptFolder.Agents.Wire(), PromptFolder.Functions.Wire(), $"{name}.md");
    }

    /// <summary>Settings that point the workspace and the skills at the test's folder.</summary>
    private AgentOptions Options() => new()
    {
        WorkspaceRoot = _skillsDir,
        SkillsDirectory = _skillsDir
    };

    /// <summary>
    /// An engine on the test's folder that can write and reload skills; a fake model unless one is given.
    /// </summary>
    private AgentEngine Engine(IChatClient? client = null)
    {
        var options = Options();
        return new AgentEngine(SkillLoader.Load(options), client ?? new FakeChatClient(), options)
        {
            SkillsDirectory = _skillsDir,
            SkillReloader = () => SkillLoader.Load(options),
        };
    }

    /// <summary>A function skill left at the folder's root, the old place, isn't loaded.</summary>
    [Test]
    public void AFunctionFileLeftAtTheRoot_IsntRead()
    {
        // CL8 step 3: function skills live in agent/functions/; an old *-skills.md at the root isn't loaded.
        File.WriteAllText(Path.Combine(_skillsDir, "farewell-skills.md"), EchoSkill.Replace("Greet", "Farewell"));

        Assert.That(SkillLoader.Load(Options()).Skills.Select(s => s.Name), Does.Not.Contain("Farewell"));
    }

    /// <summary>Where a skill file waits for the user's /approve.</summary>
    private string PendingFile(string name) => Path.Combine(_skillsDir, PromptFolder.Agents.Wire(),
        PromptFolder.Functions.Wire(), "pending", $"{name}.md");

    /// <summary>Where an approved skill file lives.</summary>
    private string ApprovedFile(string name) => Path.Combine(_skillsDir, PromptFolder.Agents.Wire(),
        PromptFolder.Functions.Wire(), $"{name}.md");

    // ---- FX20: a skill file the agent writes waits in pending/ for the user's /approve, as agent-built tools do ----

    /// <summary>A new skill file waits in pending/, and the engine that wrote it can already run it.</summary>
    [Test]
    public void CreateSkillFile_SavesLoadsAndMakesSkillExecutable()
    {
        var engine = Engine();

        var result = engine.CreateSkillFile("greeting", EchoSkill);

        Assert.That(result, Does.Contain("Greet").And.Contain("/approve greeting"));
        Assert.That(File.Exists(PendingFile("greeting")), Is.True, "written to pending/");
        Assert.That(File.Exists(ApprovedFile("greeting")), Is.False, "not approved yet");
        Assert.That(result,
            Does.Contain(Path.Combine(PromptFolder.Agents.Wire(), PromptFolder.Functions.Wire(), "pending",
                "greeting.md")));
        Assert.That(engine.SkillDocumentation, Does.Contain("# Greeting Skills"));
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hello Louis"),
            "active in the engine that wrote it");
    }

    /// <summary>Until it's approved, a later session doesn't load the skill.</summary>
    [Test]
    public void APendingSkillFile_IsntLoadedByALaterSession()
    {
        Engine().CreateSkillFile("greeting", EchoSkill);

        var later = Engine();

        Assert.That(later.SkillDocumentation, Does.Not.Contain("# Greeting Skills"));
        Assert.That(later.ExecuteSkill("Greet", """{"who":"Louis"}"""), Does.Contain("not found").IgnoreCase);
    }

    /// <summary>/approve moves the file into place: later sessions load it, this one keeps it, and only once.</summary>
    [Test]
    public void Approve_MovesTheFile_ALaterSessionSeesIt_AndThisOneKeepsItWithoutAReload()
    {
        var engine = Engine();
        engine.CreateSkillFile("greeting", EchoSkill);

        string reply = engine.HandleUserCommand("/approve greeting")!;

        Assert.That(reply, Does.Contain("Approved").And.Contain("greeting"));
        Assert.That(File.Exists(ApprovedFile("greeting")), Is.True);
        Assert.That(File.Exists(PendingFile("greeting")), Is.False);
        Assert.That(Engine().ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hello Louis"),
            "a later session loads it");
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hello Louis"),
            "this one keeps it");
        engine.ReloadSkills();
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hello Louis"),
            "and still finds it at its new path");
        Assert.That(Count(engine.SkillDocumentation, "# Greeting Skills"), Is.EqualTo(1), "loaded once, not twice");
    }

    /// <summary>
    /// /approve and /reject find a skill file whatever case the name is typed in, since the file is saved lowercase.
    /// </summary>
    [Test]
    public void Approve_AndReject_TakeTheNameInAnyCase()
    {
        var engine = Engine();
        engine.CreateSkillFile("Greeting", EchoSkill);

        Assert.That(engine.HandleUserCommand("/tools"), Does.Contain("greeting"));
        Assert.That(engine.HandleUserCommand("/approve Greeting"), Does.Contain("Approved"));
        Assert.That(File.Exists(ApprovedFile("greeting")), Is.True);

        engine.CreateSkillFile("greeting", EchoSkill.Replace("hello", "hi"), overwrite: true);
        Assert.That(engine.HandleUserCommand("/reject GREETING"), Does.Contain("Rejected"));
        Assert.That(File.Exists(PendingFile("greeting")), Is.False);
    }

    /// <summary>/reject deletes the waiting file and takes the skill out of this session too.</summary>
    [Test]
    public void Reject_DeletesTheFile_AndUnloadsItHere()
    {
        var engine = Engine();
        engine.CreateSkillFile("greeting", EchoSkill);

        string reply = engine.HandleUserCommand("/reject greeting")!;

        Assert.That(reply, Does.Contain("Rejected").And.Contain("greeting"));
        Assert.That(File.Exists(PendingFile("greeting")), Is.False);
        Assert.That(File.Exists(ApprovedFile("greeting")), Is.False);
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis"}"""), Does.Contain("not found").IgnoreCase);
    }

    /// <summary>Approving or rejecting a name nothing waits under says so.</summary>
    [Test]
    public void Approve_OfAnUnknownName_SaysSo()
    {
        Assert.That(Engine().HandleUserCommand("/approve nothing"),
            Does.Contain("No pending tool or skill file named 'nothing'"));
        Assert.That(Engine().HandleUserCommand("/reject nothing"),
            Does.Contain("No pending tool or skill file named 'nothing'"));
    }

    /// <summary>A name that's both a waiting tool and a waiting skill file is refused, and nothing moves.</summary>
    [Test]
    public void AName_ThatIsBothAPendingTool_AndAPendingSkillFile_IsRefused()
    {
        var engine = Engine();
        engine.CreateSkillFile("greeting", EchoSkill);
        Directory.CreateDirectory(Path.Combine(_skillsDir, "tools", "pending"));
        File.WriteAllText(Path.Combine(_skillsDir, "tools", "pending", "greeting.tool.md"),
            "# Tool: greeting\n- Description: x\n");

        string reply = engine.HandleUserCommand("/approve greeting")!;

        Assert.That(reply, Does.Contain("both").And.Contain("greeting.tool.md").And.Contain("greeting.md"));
        Assert.That(File.Exists(PendingFile("greeting")), Is.True, "nothing moved");
        Assert.That(File.Exists(ApprovedFile("greeting")), Is.False);
    }

    /// <summary>
    /// Overwriting an approved skill keeps the approved one for later sessions until the new one is approved.
    /// </summary>
    [Test]
    public void Overwrite_KeepsTheApprovedVersion_UntilTheNewOneIsApproved()
    {
        var engine = Engine();
        engine.CreateSkillFile("greeting", EchoSkill);
        engine.HandleUserCommand("/approve greeting");

        Assert.That(engine.CreateSkillFile("greeting", EchoSkill.Replace("hello", "hi"), overwrite: true),
            Does.Contain("/approve greeting"));

        Assert.That(File.ReadAllText(PendingFile("greeting")), Does.Contain("hi"));
        Assert.That(File.ReadAllText(ApprovedFile("greeting")), Does.Contain("hello"), "the approved version stays");
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hi Louis"),
            "the engine that wrote it runs the new one");
        Assert.That(Engine().ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hello Louis"),
            "a later session runs the approved one");

        engine.HandleUserCommand("/approve greeting");

        Assert.That(File.Exists(PendingFile("greeting")), Is.False);
        Assert.That(Engine().ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hi Louis"));
    }

    /// <summary>/tools lists the waiting skill files, and says which this session already runs.</summary>
    [Test]
    public void Tools_ListsPendingSkillFiles()
    {
        var engine = Engine();
        engine.CreateSkillFile("greeting", EchoSkill);
        File.WriteAllText(PendingFile("older"), EchoSkill.Replace("Greet", "Older"));

        string reply = engine.HandleUserCommand("/tools")!;

        Assert.That(reply, Does.Contain("greeting").And.Contain("skill file").And.Contain("active here"));
        Assert.That(reply, Does.Contain("older").And.Contain("not loaded"));
    }

    /// <summary>How many times a part appears in a text.</summary>
    private static int Count(string text, string part) => (text.Length - text.Replace(part, "").Length) / part.Length;

    /// <summary>A file with no valid skill in it is refused and not saved.</summary>
    [Test]
    public void CreateSkillFile_RejectsInvalidSkillSections()
    {
        const string missingExecution = "## Skill: Broken\n\n- Description: no execution block\n";

        var result = Engine().CreateSkillFile("broken", missingExecution);

        Assert.That(result, Does.Contain("only 0 are valid"));
        Assert.That(
            File.Exists(
                Path.Combine(_skillsDir, PromptFolder.Agents.Wire(), PromptFolder.Functions.Wire(), "broken.md")),
            Is.False);
    }

    /// <summary>A toolset's name is built in, so a skill file can't take it.</summary>
    [TestCase(TestToolset.DotNet)]
    [TestCase(TestToolset.Python)]
    public void CreateSkillFile_RefusesBuiltInNames(TestToolset toolset) => AssertRefusedAsBuiltIn(toolset.Wire());

    /// <summary>A guide's own file name is built in too.</summary>
    [TestCase("self-extension")]
    public void CreateSkillFile_RefusesBuiltInNames_ThatAreAGuidesOwnFile(string name) => AssertRefusedAsBuiltIn(name);

    /// <summary>Saving a skill file under the name is refused as built in.</summary>
    private void AssertRefusedAsBuiltIn(string name)
    {
        Assert.That(Engine().CreateSkillFile(name, EchoSkill), Does.Contain("built-in"));
    }

    /// <summary>"default" is no longer special, so a skill file may take it.</summary>
    [Test]
    public void CreateSkillFile_MayTakeTheNameDefault()
    {
        // CL10: default.md went in CL9 (the router is part of a mode), so the name isn't special any more.
        Assert.That(Engine().CreateSkillFile("default", EchoSkill), Does.Contain("Saved"));
    }

    /// <summary>"time-logging" isn't built in any more, so a skill file may take it.</summary>
    [Test]
    public void CreateSkillFile_MayTakeTheNameTimeLoggingHad()
    {
        // CL8 step 6: time-logging isn't built in any more, so the name is free for a skill of the agent's own.
        Assert.That(Engine().CreateSkillFile("time-logging", EchoSkill), Does.Contain("Saved"));
    }

    /// <summary>
    /// A function skill the agent wrote loads with the skills toolset, with no setting to turn it on.
    /// </summary>
    [Test]
    public void AnAgentWrittenFunction_LoadsWithTheSkillsToolset_NoSettingNeeded()
    {
        // CL9 step 4: AGENT_FUNCTION goes; with the skills toolset a session loads every function skill the agent
        // wrote.
        File.WriteAllText(FunctionFile("greeting"), EchoSkill);
        var options = new AgentOptions
        {
            WorkspaceRoot = _skillsDir,
            SkillsDirectory = _skillsDir
        };

        Assert.That(SkillLoader.Load(options).Skills.Select(s => s.Name), Does.Contain("Greet"));
    }

    /// <summary>Every toolset's guide name is reserved, with no list to keep by hand.</summary>
    [Test]
    public void CreateSkillFile_RefusesEveryToolsetsGuideName()
    {
        // CL9 step 4: what is embedded is built in, so a new toolset's guide is reserved with no list to edit.
        var engine = Engine();
        foreach (string guide in ToolsetCatalog.All.Select(t => t.GuideFile!))
        {
            string name = Path.GetFileNameWithoutExtension(guide);
            Assert.That(engine.CreateSkillFile(name, EchoSkill), Does.Contain("built-in"),
                $"{name} is a toolset's guide");
        }
    }

    /// <summary>A name that isn't kebab-case, one that climbs out of the folder too, is refused.</summary>
    [TestCase("../escape")]
    [TestCase("Has Spaces")]
    [TestCase("")]
    public void CreateSkillFile_RejectsUnsafeNames(string name)
    {
        Assert.That(Engine().CreateSkillFile(name, EchoSkill), Does.Contain("kebab-case"));
    }

    /// <summary>A skill file is only replaced when the call says overwrite.</summary>
    [Test]
    public void CreateSkillFile_RequiresOverwriteToReplace()
    {
        var engine = Engine();
        engine.CreateSkillFile("greeting", EchoSkill);

        Assert.That(engine.CreateSkillFile("greeting", EchoSkill), Does.Contain("already exists"));
        Assert.That(engine.CreateSkillFile("greeting", EchoSkill.Replace("hello", "hi"), overwrite: true),
            Does.Contain("Saved"));
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hi Louis"));
    }

    /// <summary>A reload finds a skill file put on disk after the engine started.</summary>
    [Test]
    public void ReloadSkills_PicksUpFilesAddedOnDisk()
    {
        var engine = Engine();
        File.WriteAllText(FunctionFile("greeting"), EchoSkill);

        Assert.That(engine.ReloadSkills(), Does.Contain("Greet"));
    }

    /// <summary>
    /// A skill written mid-chat leaves the open chat's system prompt as it was sent; the next chat gets the new one.
    /// </summary>
    [Test]
    public async Task ProcessPrompt_KeepsTheSystemPromptItStarted_TheNextConversationGetsTheNewOne()
    {
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "first")),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "second")),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "third")));
        var engine = Engine(client: client);
        var open = engine.StartConversation();

        await engine.ProcessPromptAsync(open, "before");
        engine.CreateSkillFile("greeting", EchoSkill);
        await engine.ProcessPromptAsync(open, "after");
        await engine.ProcessPromptAsync(engine.StartConversation(), "a new chat");

        Assert.That(client.Requests[1].Messages[0].Text, Is.EqualTo(client.Requests[0].Messages[0].Text),
            "the open chat's prompt is as it was sent");
        Assert.That(client.Requests[2].Messages[0].Text, Does.Contain("# Greeting Skills"), "the next chat's has it");
    }
}
