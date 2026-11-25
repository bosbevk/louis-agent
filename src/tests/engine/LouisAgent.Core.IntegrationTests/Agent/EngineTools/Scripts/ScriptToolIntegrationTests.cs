using LouisAgent.Composition.Tools;
using LouisAgent.Testing;
using LouisAgent.Choices;
using LouisAgent.Core.Skills.Languages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Hosting.Prompts;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Agent.EngineTools.Scripts;
using LouisAgent.Composition.Tools.Scripts;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Core.IntegrationTests.Agent.EngineTools.Scripts;

/// <summary>
/// A tool the agent writes as a script really runs: its arguments arrive as JSON on stdin, in Python, PowerShell or
/// bash, and the model can call it in the same turn that made it.
/// </summary>
[Category("Integration")]
public class ScriptToolIntegrationTests
{
    private const string AddTool = """
                                   # Tool: AddNumbers
                                   - Description: Add two integers
                                   - Timeout: 30
                                   - Parameters:
                                     - `a` (integer, required): first number
                                     - `b` (integer, optional): second number, default 0
                                   - Implementation:

                                   ```python
                                   import json, sys
                                   args = json.load(sys.stdin)
                                   print(json.dumps({"sum": args["a"] + args.get("b", 0)}))
                                   ```
                                   """;

    private string _skillsDir = null!;

    /// <summary>Each test gets an empty skills folder of its own.</summary>
    [SetUp]
    public void SetUp()
    {
        _skillsDir = Directory.CreateTempSubdirectory("script-tools-").FullName;
    }

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    /// <summary>An engine on the test's folder that can make tools; a fake model unless one is given.</summary>
    private AgentEngine Engine(IChatClient? client = null)
    {
        var options = new AgentOptions
        {
            WorkspaceRoot = _skillsDir,
            SkillsDirectory = _skillsDir
        };
        var engine = new AgentEngine(SkillLoader.Load(options), client ?? new FakeChatClient(), options)
        {
            SkillsDirectory = _skillsDir,
            SkillReloader = () => SkillLoader.Load(options),
        };
        engine.LoadApprovedTools();
        return engine;
    }

    /// <summary>Skips the test, saying why, on a machine without Python 3.</summary>
    private static void RequirePython() =>
        Outside.Tool(new PythonRunner(Path.GetTempPath()).ResolveInterpreter() is not null,
            "Python 3 is not installed");

    /// <summary>The engine's tool by name, as the function the model would call.</summary>
    private static AIFunction Tool(AgentEngine engine, string name) =>
        engine.Tools.OfType<AIFunction>().Single(t => t.Name == name);

    /// <summary>A Python tool the agent made runs, its arguments given as JSON on stdin.</summary>
    [Test]
    public async Task CreatedTool_RunsWithArgumentsAsJsonOnStdin()
    {
        RequirePython();
        var engine = Engine();
        engine.CreateTool(AddTool);

        var result = await Tool(engine, "AddNumbers").InvokeAsync(new AIFunctionArguments
        {
            ["a"] = 2,
            ["b"] = 40
        });

        Assert.That(result?.ToString(), Is.EqualTo("""{"sum": 42}"""));
    }

    /// <summary>A tool written in PowerShell or bash runs too, each reading its arguments its own way.</summary>
    [TestCase(ScriptLanguage.PowerShell, "$a = [Console]::In.ReadToEnd() | ConvertFrom-Json; \"echo:$($a.text)\"")]
    [TestCase(ScriptLanguage.Bash, "echo \"echo:$TOOL_ARG_text\"")]
    public async Task CreatedTool_SupportsPowerShellAndBash(ScriptLanguage language, string code)
    {
        if (language == ScriptLanguage.PowerShell)
            Outside.Tool(new PowerShellRunner(_skillsDir).ResolveInterpreter() is not null,
                "PowerShell is not installed");
        var engine = Engine();
        string definition =
            "# Tool: EchoText\n- Description: Echo\n- Parameters:\n  - `text` (string, required): text\n" +
            $"- Implementation:\n\n```{language.Wire()}\n{code}\n```\n";
        Assert.That(engine.CreateTool(definition), Does.Contain("Created"));

        var result = await Tool(engine, "EchoText")
            .InvokeAsync(new AIFunctionArguments { ["text"] = "a\"; rm -rf / #" });

        Assert.That(result?.ToString(), Is.EqualTo("echo:a\"; rm -rf / #"),
            "arguments must arrive as data, never as code");
    }

    /// <summary>The model can call a tool in the same turn it made it.</summary>
    [Test]
    public async Task NewTool_IsCallableInTheSameTurnThatCreatedIt()
    {
        RequirePython();
        string escapedDefinition = JsonSerializer.Serialize(AddTool);
        var client = new FakeChatClient(
            _ => ToolCall("c1", "CreateTool", $$"""{"definition":{{escapedDefinition}}}"""),
            _ => ToolCall("c2", "AddNumbers", """{"a":20,"b":22}"""),
            messages => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                "result: " + messages.Last().Contents.OfType<FunctionResultContent>().Single().Result)));
        var engine = Engine(client);

        string answer = await engine.ProcessPromptAsync(engine.StartConversation(), "add 20 and 22 with a new tool");

        Assert.That(answer, Is.EqualTo("""result: {"sum": 42}"""));
    }

    /// <summary>The model's answer that calls the tool named, with the arguments given as JSON.</summary>
    private static ChatResponse ToolCall(string callId, string name, string argumentsJson)
    {
        var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson);
        return new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [new FunctionCallContent(callId, name, arguments)]));
    }
}
