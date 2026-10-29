using LouisAgent.Testing;
using LouisAgent.Choices;
using LouisAgent.Core.Skills.Languages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Hosting.Prompts;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Tools.Scripts;
using LouisAgent.Core.Tools.Shell;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Core.IntegrationTests.Tools.Scripts;

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

    [SetUp]
    public void SetUp()
    {
        _skillsDir = Directory.CreateTempSubdirectory("script-tools-").FullName;
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    private AgentEngine Engine(IChatClient? client = null)
    {
        var options = new AgentOptions { WorkspaceRoot = _skillsDir, SkillsDirectory = _skillsDir };
        var engine = new AgentEngine(SkillLoader.Load(options), client ?? new FakeChatClient(), options)
        {
            SkillsDirectory = _skillsDir,
            SkillReloader = () => SkillLoader.Load(options),
        };
        engine.LoadApprovedTools();
        return engine;
    }

    private static void RequirePython() =>
        Outside.Tool(new PythonTools(Path.GetTempPath()).ResolveInterpreter() is not null, "Python 3 is not installed");

    private static AIFunction Tool(AgentEngine engine, string name) =>
        engine.Tools.OfType<AIFunction>().Single(t => t.Name == name);

    [Test]
    public async Task CreatedTool_RunsWithArgumentsAsJsonOnStdin()
    {
        RequirePython();
        var engine = Engine();
        engine.CreateTool(AddTool);

        var result = await Tool(engine, "AddNumbers").InvokeAsync(new AIFunctionArguments { ["a"] = 2, ["b"] = 40 });

        Assert.That(result?.ToString(), Is.EqualTo("""{"sum": 42}"""));
    }

    [TestCase(ScriptLanguage.PowerShell, "$a = [Console]::In.ReadToEnd() | ConvertFrom-Json; \"echo:$($a.text)\"")]
    [TestCase(ScriptLanguage.Bash, "echo \"echo:$TOOL_ARG_text\"")]
    public async Task CreatedTool_SupportsPowerShellAndBash(ScriptLanguage language, string code)
    {
        if (language == ScriptLanguage.PowerShell)
            Outside.Tool(new PowerShellTools(_skillsDir).ResolveInterpreter() is not null,
                "PowerShell is not installed");
        var engine = Engine();
        string definition =
            $"# Tool: EchoText\n- Description: Echo\n- Parameters:\n  - `text` (string, required): text\n- Implementation:\n\n```{language.Wire()}\n{code}\n```\n";
        Assert.That(engine.CreateTool(definition), Does.Contain("Created"));

        var result = await Tool(engine, "EchoText")
            .InvokeAsync(new AIFunctionArguments { ["text"] = "a\"; rm -rf / #" });

        Assert.That(result?.ToString(), Is.EqualTo("echo:a\"; rm -rf / #"),
            "arguments must arrive as data, never as code");
    }

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

        string answer = await engine.ProcessPromptAsync(engine.NewHistory(), "add 20 and 22 with a new tool");

        Assert.That(answer, Is.EqualTo("""result: {"sum": 42}"""));
    }

    private static ChatResponse ToolCall(string callId, string name, string argumentsJson)
    {
        var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson);
        return new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [new FunctionCallContent(callId, name, arguments)]));
    }
}
