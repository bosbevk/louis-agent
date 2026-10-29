using LouisAgent.Testing;
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

namespace LouisAgent.Core.IntegrationTests.Agent.EngineTools;

[Category("Integration")]
public class SkillAuthoringIntegrationTests
{
    private string _skillsDir = null!;

    [SetUp]
    public void SetUp()
    {
        _skillsDir = Directory.CreateTempSubdirectory("skills-").FullName;
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

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

    [TestCase(ScriptLanguage.PowerShell, "\"hello $env:SKILL_ARG_who\"")]
    [TestCase(ScriptLanguage.PowerShell, "\"hello {{who}}\"")]
    [TestCase(ScriptLanguage.Python, "import os\nprint('hello', os.environ['SKILL_ARG_who'])")]
    [TestCase(ScriptLanguage.Python, "print('hello', {{who}})")]
    public void ExecuteSkill_RunsSkillInItsFenceLanguage(ScriptLanguage language, string code)
    {
        if (language == ScriptLanguage.Python)
            Outside.Tool(new PythonTools(_skillsDir).ResolveInterpreter() is not null, "Python 3 is not installed");
        if (language == ScriptLanguage.PowerShell)
            Outside.Tool(new PowerShellTools(_skillsDir).ResolveInterpreter() is not null,
                "PowerShell is not installed");
        var engine = Engine();
        string markdown =
            $"## Skill: Greet\n\n- Description: Print a greeting\n- Execution:\n\n```{language.Wire()}\n{code}\n```\n";

        Assert.That(engine.CreateSkillFile("greeting", markdown), Does.Contain("Saved"));
        Assert.That(engine.ExecuteSkill("Greet", """{"who":"Louis; rm -rf /"}"""), Is.EqualTo("hello Louis; rm -rf /"),
            "arguments must arrive as data, never as code");
    }
}
