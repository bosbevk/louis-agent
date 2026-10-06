using louis_agent.core.config;
using louis_agent.core.providers;
using louis_agent.core.tools;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

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
        File.WriteAllText(Path.Combine(_skillsDir, "default.md"), "# Default instructions");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    private AgentOptions Options(string function) =>
        new() { WorkspaceRoot = _skillsDir, SkillsDirectory = _skillsDir, AgentFunction = function };

    private AgentEngine Engine(string function = "code-review", IChatClient? client = null)
    {
        var options = Options(function);
        return new AgentEngine(AgentHost.LoadSkills(options), client ?? new FakeChatClient(), options)
        {
            SkillsDirectory = _skillsDir,
            SkillReloader = () => AgentHost.LoadSkills(options),
        };
    }

    [Test]
    public void LoadSkills_LouisFunction_DiscoversCustomSkillFiles()
    {
        File.WriteAllText(Path.Combine(_skillsDir, "greeting-skills.md"), EchoSkill);

        var louis = AgentHost.LoadSkills(Options("louis"));
        var codeReview = AgentHost.LoadSkills(Options("code-review"));

        Assert.That(louis.Skills.Select(s => s.Name), Does.Contain("Greet"));
        Assert.That(codeReview.Skills.Select(s => s.Name), Does.Not.Contain("Greet"), "other functions only load their own file");
    }

    [Test]
    public void CreateSkillFile_SavesLoadsAndMakesSkillExecutable()
    {
        var engine = Engine();

        var result = engine.CreateSkillFile("greeting", EchoSkill);

        Assert.That(result, Does.Contain("Greet"));
        Assert.That(File.Exists(Path.Combine(_skillsDir, "greeting-skills.md")), Is.True);
        Assert.That(engine.SkillDocumentation, Does.Contain("# Greeting Skills"));
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis"}"""), Is.EqualTo("hello Louis"));
    }

    [Test]
    public void CreateSkillFile_RejectsInvalidSkillSections()
    {
        const string missingExecution = "## Skill: Broken\n\n- Description: no execution block\n";

        var result = Engine().CreateSkillFile("broken", missingExecution);

        Assert.That(result, Does.Contain("only 0 are valid"));
        Assert.That(File.Exists(Path.Combine(_skillsDir, "broken-skills.md")), Is.False);
    }

    [TestCase("dotnet")]
    [TestCase("python")]
    [TestCase("paymo")]
    [TestCase("default")]
    public void CreateSkillFile_RefusesBuiltInNames(string name)
    {
        Assert.That(Engine().CreateSkillFile(name, EchoSkill), Does.Contain("built-in"));
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

    [TestCase("powershell", "\"hello $env:SKILL_ARG_who\"")]
    [TestCase("powershell", "\"hello {{who}}\"")]
    [TestCase("python", "import os\nprint('hello', os.environ['SKILL_ARG_who'])")]
    [TestCase("python", "print('hello', {{who}})")]
    [Category("Integration")]
    public void ExecuteSkill_RunsSkillInItsFenceLanguage(string language, string code)
    {
        if (language == "python") Assume.That(new PythonTools(_skillsDir).ResolveInterpreter(), Is.Not.Null);
        if (language == "powershell") Assume.That(new PowerShellTools(_skillsDir).ResolveInterpreter(), Is.Not.Null);
        var engine = Engine();
        string markdown = $"## Skill: Greet\n\n- Description: Print a greeting\n- Execution:\n\n```{language}\n{code}\n```\n";

        Assert.That(engine.CreateSkillFile("greeting", markdown), Does.Contain("Saved"));
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis; rm -rf /"}"""), Is.EqualTo("hello Louis; rm -rf /"),
            "arguments must arrive as data, never as code");
    }

    [Test]
    public void ReloadSkills_PicksUpFilesAddedOnDisk()
    {
        var engine = Engine("louis");
        File.WriteAllText(Path.Combine(_skillsDir, "greeting-skills.md"), EchoSkill);

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
