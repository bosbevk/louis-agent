using System.Text.Json;
using LouisAgent.Composition.Tools.Scripts;
using LouisAgent.Core.Prompting.Headers.Agent;
using LouisAgent.Core.Prompting.Headers.Agent.Running;
using LouisAgent.Core.Prompting.Languages;

namespace LouisAgent.Core.IntegrationTests.Prompting.Headers.Agent.Running;

/// <summary>
/// F97.1 step 4: a skill's script really runs in each language core has, reading the call's arguments as JSON from
/// stdin, and what it prints is the answer. A machine without the language skips; CI, which has all three, fails.
/// </summary>
[Category("Integration")]
public class SkillRunnerIntegrationTests
{
    private string _workspace = "";
    private SkillRunner _runner = null!;

    /// <summary>A workspace of its own, and core's three runners over it.</summary>
    [SetUp]
    public void MakeTheWorkspace()
    {
        _workspace = Directory.CreateTempSubdirectory("skill-runner-").FullName;
        _runner = SkillRunner.For(new PythonRunner(_workspace), new PowerShellRunner(_workspace),
            new BashRunner(_workspace));
    }

    /// <summary>The workspace goes with the test.</summary>
    [TearDown]
    public void RemoveTheWorkspace() => Directory.Delete(_workspace, recursive: true);

    /// <summary>Each language's script reads the text from stdin and prints how many words it has.</summary>
    [TestCase(ScriptLanguage.Python, "count.py",
        "import json, sys\nprint(len(json.load(sys.stdin)['text'].split()))\n")]
    [TestCase(ScriptLanguage.PowerShell, "count.ps1",
        "$text = ([Console]::In.ReadToEnd() | ConvertFrom-Json).text\n" +
        "($text -split '\\s+' | Where-Object { $_ }).Count\n")]
    [TestCase(ScriptLanguage.Bash, "count.sh",
        "read -r line\necho \"$line\" | sed 's/.*\"text\": *\"\\([^\"]*\\)\".*/\\1/' | wc -w | tr -d ' '\n")]
    public void AScript_ReadsItsArgumentsFromStdin(ScriptLanguage language, string file, string code)
    {
        RequireThe(language);
        string script = Path.Combine(_workspace, file);
        File.WriteAllText(script, code);
        var skill = new Skill("word-count", "Counts words.", "", _workspace, script, language,
            new SkillParameters(Json("{}"), new Dictionary<string, SkillParameterType>(), []), SkillState.Drafted);

        var answer = _runner.Run(skill, Json("""{ "text": "one two three" }"""));

        Assert.That(answer, Is.EqualTo(new SkillAnswer(true, "3")), answer.Text);
    }

    /// <summary>
    /// An argument full of shell syntax reaches the script as data on stdin and is never run (ADR 0004): the script
    /// prints it back, and the file it tries to make isn't there.
    /// </summary>
    [Test]
    public void AnArgumentOfShellSyntax_IsReadAsData_NeverRun()
    {
        RequireThe(ScriptLanguage.Bash);
        string marker = Path.Combine(_workspace, "injected").Replace('\\', '/');
        string script = Path.Combine(_workspace, "echo.sh");
        File.WriteAllText(script, "cat\n");
        var skill = new Skill("echo", "Prints its input.", "", _workspace, script, ScriptLanguage.Bash,
            new SkillParameters(Json("{}"), new Dictionary<string, SkillParameterType>(), []), SkillState.Drafted);
        var payload = new Dictionary<string, string>
        {
            ["text"] = $"x\"; touch '{marker}'; echo \"$(touch '{marker}')`touch '{marker}'`"
        };

        var answer = _runner.Run(skill, JsonSerializer.SerializeToElement(payload));

        Assert.That(File.Exists(marker), Is.False, "the payload must not run");
        Assert.That(answer.Text, Does.Contain("touch"), "the payload comes back as text");
    }

    /// <summary>Skips, saying why, where the language isn't installed; fails on CI, which has all three.</summary>
    private void RequireThe(ScriptLanguage language)
    {
        bool installed = language switch
        {
            ScriptLanguage.Python => new PythonRunner(_workspace).ResolveInterpreter() is not null,
            ScriptLanguage.PowerShell => new PowerShellRunner(_workspace).ResolveInterpreter() is not null,
            ScriptLanguage.Bash => BashRunner.FindBash() is not null,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };
        Outside.Tool(installed, $"{language} is not installed");
    }

    /// <summary>A call's arguments as the model sends them.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;
}
