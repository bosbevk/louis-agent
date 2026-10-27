using LouisAgent.Choices;
using LouisAgent.Core.Skills.Languages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Tools.Shell;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Core.Tests.Agent.EngineTools;

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

    [SetUp]
    public void SetUp()
    {
        _skillsDir = Directory.CreateTempSubdirectory("skills-").FullName;
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    /// <summary>A function skill file (CL8: agent/functions/{name}.md), as a user would drop one in.</summary>
    private string FunctionFile(string name)
    {
        Directory.CreateDirectory(Path.Combine(_skillsDir, "agent", "functions"));
        return Path.Combine(_skillsDir, "agent", "functions", $"{name}.md");
    }

    private AgentOptions Options() => new() { WorkspaceRoot = _skillsDir, SkillsDirectory = _skillsDir };

    private AgentEngine Engine(IChatClient? client = null)
    {
        var options = Options();
        return new AgentEngine(SkillLoader.Load(options), client ?? new FakeChatClient(), options)
        {
            SkillsDirectory = _skillsDir,
            SkillReloader = () => SkillLoader.Load(options),
        };
    }

    [Test]
    public void AFunctionFileLeftAtTheRoot_IsntRead()
    {
        // CL8 step 3: function skills live in agent/functions/; an old *-skills.md at the root isn't loaded.
        File.WriteAllText(Path.Combine(_skillsDir, "farewell-skills.md"), EchoSkill.Replace("Greet", "Farewell"));

        Assert.That(SkillLoader.Load(Options()).Skills.Select(s => s.Name), Does.Not.Contain("Farewell"));
    }

    private string PendingFile(string name) => Path.Combine(_skillsDir, "agent", "functions", "pending", $"{name}.md");
    private string ApprovedFile(string name) => Path.Combine(_skillsDir, "agent", "functions", $"{name}.md");

    // ---- FX20: a skill file the agent writes waits in pending/ for the user's /approve, as agent-built tools do ----

    [Test]
    public void CreateSkillFile_SavesLoadsAndMakesSkillExecutable()
    {
        var engine = Engine();

        var result = engine.CreateSkillFile("greeting", EchoSkill);

        Assert.That(result, Does.Contain("Greet").And.Contain("/approve greeting"));
        Assert.That(File.Exists(PendingFile("greeting")), Is.True, "written to pending/");
        Assert.That(File.Exists(ApprovedFile("greeting")), Is.False, "not approved yet");
        Assert.That(result, Does.Contain(Path.Combine("agent", "functions", "pending", "greeting.md")));
        Assert.That(engine.SkillDocumentation, Does.Contain("# Greeting Skills"));
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hello Louis"), "active in the engine that wrote it");
    }

    [Test]
    public void APendingSkillFile_IsntLoadedByALaterSession()
    {
        Engine().CreateSkillFile("greeting", EchoSkill);

        var later = Engine();

        Assert.That(later.SkillDocumentation, Does.Not.Contain("# Greeting Skills"));
        Assert.That(later.ExecuteSkill("Greet", """{"who":"Louis"}"""), Does.Contain("not found").IgnoreCase);
    }

    [Test]
    public void Approve_MovesTheFile_ALaterSessionSeesIt_AndThisOneKeepsItWithoutAReload()
    {
        var engine = Engine();
        engine.CreateSkillFile("greeting", EchoSkill);

        string reply = engine.HandleUserCommand("/approve greeting")!;

        Assert.That(reply, Does.Contain("Approved").And.Contain("greeting"));
        Assert.That(File.Exists(ApprovedFile("greeting")), Is.True);
        Assert.That(File.Exists(PendingFile("greeting")), Is.False);
        Assert.That(Engine().ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hello Louis"), "a later session loads it");
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hello Louis"), "this one keeps it");
        engine.ReloadSkills();
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hello Louis"), "and still finds it at its new path");
        Assert.That(CountOf(engine.SkillDocumentation, "# Greeting Skills"), Is.EqualTo(1), "loaded once, not twice");
    }

    // Review of PR #68: CreateSkillFile lowercases the name, so /approve has to as well, or on Linux "Greeting" finds nothing.
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

    [Test]
    public void Approve_OfAnUnknownName_SaysSo()
    {
        Assert.That(Engine().HandleUserCommand("/approve nothing"), Does.Contain("No pending tool or skill file named 'nothing'"));
        Assert.That(Engine().HandleUserCommand("/reject nothing"), Does.Contain("No pending tool or skill file named 'nothing'"));
    }

    [Test]
    public void AName_ThatIsBothAPendingTool_AndAPendingSkillFile_IsRefused()
    {
        var engine = Engine();
        engine.CreateSkillFile("greeting", EchoSkill);
        Directory.CreateDirectory(Path.Combine(_skillsDir, "tools", "pending"));
        File.WriteAllText(Path.Combine(_skillsDir, "tools", "pending", "greeting.tool.md"), "# Tool: greeting\n- Description: x\n");

        string reply = engine.HandleUserCommand("/approve greeting")!;

        Assert.That(reply, Does.Contain("both").And.Contain("greeting.tool.md").And.Contain("greeting.md"));
        Assert.That(File.Exists(PendingFile("greeting")), Is.True, "nothing moved");
        Assert.That(File.Exists(ApprovedFile("greeting")), Is.False);
    }

    [Test]
    public void Overwrite_KeepsTheApprovedVersion_UntilTheNewOneIsApproved()
    {
        var engine = Engine();
        engine.CreateSkillFile("greeting", EchoSkill);
        engine.HandleUserCommand("/approve greeting");

        Assert.That(engine.CreateSkillFile("greeting", EchoSkill.Replace("hello", "hi"), overwrite: true), Does.Contain("/approve greeting"));

        Assert.That(File.ReadAllText(PendingFile("greeting")), Does.Contain("hi"));
        Assert.That(File.ReadAllText(ApprovedFile("greeting")), Does.Contain("hello"), "the approved version stays");
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hi Louis"), "the engine that wrote it runs the new one");
        Assert.That(Engine().ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hello Louis"), "a later session runs the approved one");

        engine.HandleUserCommand("/approve greeting");

        Assert.That(File.Exists(PendingFile("greeting")), Is.False);
        Assert.That(Engine().ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hi Louis"));
    }

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

    private static int CountOf(string text, string part) => (text.Length - text.Replace(part, "").Length) / part.Length;

    [Test]
    public void CreateSkillFile_RejectsInvalidSkillSections()
    {
        const string missingExecution = "## Skill: Broken\n\n- Description: no execution block\n";

        var result = Engine().CreateSkillFile("broken", missingExecution);

        Assert.That(result, Does.Contain("only 0 are valid"));
        Assert.That(File.Exists(Path.Combine(_skillsDir, "agent", "functions", "broken.md")), Is.False);
    }

    [TestCase(CoreToolset.DotNet)]
    [TestCase(CoreToolset.Python)]
    [TestCase(CoreToolset.Paymo)]
    public void CreateSkillFile_RefusesBuiltInNames(CoreToolset toolset) => RefusesAsBuiltIn(toolset.Wire());

    [TestCase("self-extension")]
    public void CreateSkillFile_RefusesBuiltInNames_ThatAreAGuidesOwnFile(string name) => RefusesAsBuiltIn(name);

    private void RefusesAsBuiltIn(string name)
    {
        Assert.That(Engine().CreateSkillFile(name, EchoSkill), Does.Contain("built-in"));
    }

    [Test]
    public void CreateSkillFile_MayTakeTheNameDefault()
    {
        // CL10: default.md went in CL9 (the router is part of a mode), so the name isn't special any more.
        Assert.That(Engine().CreateSkillFile("default", EchoSkill), Does.Contain("Saved"));
    }

    [Test]
    public void CreateSkillFile_MayTakeTheNameTimeLoggingHad()
    {
        // CL8 step 6: time-logging isn't built in any more, so the name is free for a skill of the agent's own.
        Assert.That(Engine().CreateSkillFile("time-logging", EchoSkill), Does.Contain("Saved"));
    }

    [Test]
    public void AnAgentWrittenFunction_LoadsWithTheSkillsToolset_NoSettingNeeded()
    {
        // CL9 step 4: AGENT_FUNCTION goes; with the skills toolset a session loads every function skill the agent wrote.
        File.WriteAllText(FunctionFile("greeting"), EchoSkill);
        var options = new AgentOptions { WorkspaceRoot = _skillsDir, SkillsDirectory = _skillsDir };

        Assert.That(SkillLoader.Load(options).Skills.Select(s => s.Name), Does.Contain("Greet"));
    }

    [Test]
    public void CreateSkillFile_RefusesEveryToolsetsGuideName()
    {
        // CL9 step 4: what is embedded is built in, so a new toolset's guide is reserved with no list to edit.
        var engine = Engine();
        foreach (string guide in ToolsetCatalog.All.Select(t => t.GuideFile!))
        {
            string name = Path.GetFileNameWithoutExtension(guide);
            Assert.That(engine.CreateSkillFile(name, EchoSkill), Does.Contain("built-in"), $"{name} is a toolset's guide");
        }
    }

    [TestCase("../escape")]
    [TestCase("Has Spaces")]
    [TestCase("")]
    public void CreateSkillFile_RejectsUnsafeNames(string name)
    {
        Assert.That(Engine().CreateSkillFile(name, EchoSkill), Does.Contain("kebab-case"));
    }

    [Test]
    public void CreateSkillFile_RequiresOverwriteToReplace()
    {
        var engine = Engine();
        engine.CreateSkillFile("greeting", EchoSkill);

        Assert.That(engine.CreateSkillFile("greeting", EchoSkill), Does.Contain("already exists"));
        Assert.That(engine.CreateSkillFile("greeting", EchoSkill.Replace("hello", "hi"), overwrite: true), Does.Contain("Saved"));
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hi Louis"));
    }

    [TestCase(ScriptLanguage.PowerShell, "\"hello $env:SKILL_ARG_who\"")]
    [TestCase(ScriptLanguage.PowerShell, "\"hello {{who}}\"")]
    [TestCase(ScriptLanguage.Python, "import os\nprint('hello', os.environ['SKILL_ARG_who'])")]
    [TestCase(ScriptLanguage.Python, "print('hello', {{who}})")]
    [Category("Integration")]
    public void ExecuteSkill_RunsSkillInItsFenceLanguage(ScriptLanguage language, string code)
    {
        if (language == ScriptLanguage.Python) Assume.That(new PythonTools(_skillsDir).ResolveInterpreter(), Is.Not.Null);
        if (language == ScriptLanguage.PowerShell) Assume.That(new PowerShellTools(_skillsDir).ResolveInterpreter(), Is.Not.Null);
        var engine = Engine();
        string markdown = $"## Skill: Greet\n\n- Description: Print a greeting\n- Execution:\n\n```{language.Wire()}\n{code}\n```\n";

        Assert.That(engine.CreateSkillFile("greeting", markdown), Does.Contain("Saved"));
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis; rm -rf /"}"""), Is.EqualTo("hello Louis; rm -rf /"),
            "arguments must arrive as data, never as code");
    }

    [Test]
    public void ReloadSkills_PicksUpFilesAddedOnDisk()
    {
        var engine = Engine();
        File.WriteAllText(FunctionFile("greeting"), EchoSkill);

        Assert.That(engine.ReloadSkills(), Does.Contain("Greet"));
    }

    [Test]
    public async Task ProcessPrompt_UsesReloadedSystemPromptInExistingConversation()
    {
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "first")),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "second")));
        var engine = Engine(client: client);
        var history = engine.NewHistory();

        await engine.ProcessPromptAsync(history, "before");
        engine.CreateSkillFile("greeting", EchoSkill);
        await engine.ProcessPromptAsync(history, "after");

        Assert.That(client.Requests[0].Messages[0].Text, Does.Not.Contain("# Greeting Skills"));
        Assert.That(client.Requests[1].Messages[0].Text, Does.Contain("# Greeting Skills"));
    }
}
