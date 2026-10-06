using System.Text.Json;
using louis_agent.core.config;
using louis_agent.core.tools;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

public class ScriptToolTests
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
        File.WriteAllText(Path.Combine(_skillsDir, "default.md"), "# Default");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    private AgentEngine Engine(IChatClient? client = null)
    {
        var options = new AgentOptions { WorkspaceRoot = _skillsDir, SkillsDirectory = _skillsDir, AgentFunction = "code-review" };
        var engine = new AgentEngine(AgentHost.LoadSkills(options), client ?? new FakeChatClient(), options)
        {
            SkillsDirectory = _skillsDir,
            SkillReloader = () => AgentHost.LoadSkills(options),
        };
        engine.LoadApprovedTools();
        return engine;
    }

    private static void RequirePython() =>
        Assume.That(new PythonTools(Path.GetTempPath()).ResolveInterpreter(), Is.Not.Null, "Python 3 is not installed");

    private static AIFunction Tool(AgentEngine engine, string name) => engine.Tools.OfType<AIFunction>().Single(t => t.Name == name);

    [Test]
    public void Parse_ReadsNameParametersLanguageAndTimeout()
    {
        var (definition, errors) = ScriptToolDefinition.Parse(AddTool);

        Assert.That(errors, Is.Empty);
        Assert.That(definition!.Name, Is.EqualTo("AddNumbers"));
        Assert.That(definition.Language, Is.EqualTo("python"));
        Assert.That(definition.TimeoutSeconds, Is.EqualTo(30));
        Assert.That(definition.Parameters.Select(p => (p.Name, p.Type, p.Required)),
            Is.EqualTo(new[] { ("a", "integer", true), ("b", "integer", false) }));
        Assert.That(definition.Code, Does.StartWith("import json, sys"));
    }

    [Test]
    public void Parse_ReportsAllProblemsAtOnce()
    {
        const string broken = """
            # Tool: bad name
            - Parameters:
              - `x` (date, required): unsupported type
              - just text
            - Implementation:

            ```ruby
            puts 1
            ```
            """;

        var (definition, errors) = ScriptToolDefinition.Parse(broken);

        Assert.That(definition, Is.Null);
        Assert.That(errors, Has.Some.Contains("PascalCase"));
        Assert.That(errors, Has.Some.Contains("Description"));
        Assert.That(errors, Has.Some.Contains("unsupported type 'date'"));
        Assert.That(errors, Has.Some.Contains("Cannot parse parameter line"));
        Assert.That(errors, Has.Some.Contains("ruby"));
    }

    [Test]
    public void ScriptTool_ExposesJsonSchemaWithRequiredParameters()
    {
        var tool = new ScriptTool(ScriptToolDefinition.Parse(AddTool).Definition!, approved: false, (_, _, _) => "");
        var schema = tool.JsonSchema;

        Assert.That(schema.GetProperty("properties").GetProperty("a").GetProperty("type").GetString(), Is.EqualTo("integer"));
        Assert.That(schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()), Is.EqualTo(new[] { "a" }));
        Assert.That(tool.Description, Does.Contain("pending user approval"));
    }

    [Test]
    public void ScriptTool_ValidatesAndConvertsArguments()
    {
        var tool = new ScriptTool(ScriptToolDefinition.Parse(AddTool).Definition!, approved: true, (_, _, _) => "");

        Assert.That(tool.TryBuildInput(new Dictionary<string, object?> { ["a"] = "5" }, out var json, out var env, out _), Is.True,
            "numeric strings are accepted for integer parameters");
        Assert.That(json, Is.EqualTo("""{"a":5}"""));
        Assert.That(env["TOOL_ARG_a"], Is.EqualTo("5"));

        Assert.That(tool.TryBuildInput(new Dictionary<string, object?>(), out _, out _, out var missing), Is.False);
        Assert.That(missing, Does.Contain("missing required parameter 'a'"));

        Assert.That(tool.TryBuildInput(new Dictionary<string, object?> { ["a"] = "five" }, out _, out _, out var wrongType), Is.False);
        Assert.That(wrongType, Does.Contain("must be a integer"));

        Assert.That(tool.TryBuildInput(new Dictionary<string, object?> { ["a"] = 1, ["c"] = 2 }, out _, out _, out var unknown), Is.False);
        Assert.That(unknown, Does.Contain("unknown parameter 'c'"));
    }

    [Test]
    public void CreateTool_RegistersPendingToolAndWritesPendingFile()
    {
        var engine = Engine();

        var result = engine.CreateTool(AddTool);

        Assert.That(result, Does.Contain("/approve AddNumbers"));
        Assert.That(engine.Tools.Select(t => t.Name), Does.Contain("AddNumbers"));
        Assert.That(File.Exists(Path.Combine(_skillsDir, "tools", "pending", "AddNumbers.tool.md")), Is.True);
        Assert.That(File.Exists(Path.Combine(_skillsDir, "tools", "AddNumbers.tool.md")), Is.False, "not approved yet");
    }

    [Test]
    public void CreateTool_RejectsInvalidDefinitionsAndBuiltInNames()
    {
        var engine = Engine();

        Assert.That(engine.CreateTool("# Tool: Nope"), Does.Contain("invalid tool definition"));
        Assert.That(engine.CreateTool(AddTool.Replace("AddNumbers", "PythonRun")), Does.Contain("built-in tool"));
    }

    [Test]
    public void CreateTool_RequiresOverwriteToReplacePendingTool()
    {
        var engine = Engine();
        engine.CreateTool(AddTool);

        Assert.That(engine.CreateTool(AddTool), Does.Contain("overwrite=true"));
        Assert.That(engine.CreateTool(AddTool.Replace("Add two integers", "Sum"), overwrite: true), Does.Contain("Created"));
        Assert.That(engine.Tools.Count(t => t.Name == "AddNumbers"), Is.EqualTo(1));
    }

    [Test]
    [Category("Integration")]
    public async Task CreatedTool_RunsWithArgumentsAsJsonOnStdin()
    {
        RequirePython();
        var engine = Engine();
        engine.CreateTool(AddTool);

        var result = await Tool(engine, "AddNumbers").InvokeAsync(new AIFunctionArguments { ["a"] = 2, ["b"] = 40 });

        Assert.That(result?.ToString(), Is.EqualTo("""{"sum": 42}"""));
    }

    [TestCase("powershell", "$a = [Console]::In.ReadToEnd() | ConvertFrom-Json; \"echo:$($a.text)\"")]
    [TestCase("bash", "echo \"echo:$TOOL_ARG_text\"")]
    [Category("Integration")]
    public async Task CreatedTool_SupportsPowerShellAndBash(string language, string code)
    {
        if (language == "powershell") Assume.That(new PowerShellTools(_skillsDir).ResolveInterpreter(), Is.Not.Null);
        var engine = Engine();
        string definition = $"# Tool: EchoText\n- Description: Echo\n- Parameters:\n  - `text` (string, required): text\n- Implementation:\n\n```{language}\n{code}\n```\n";
        Assert.That(engine.CreateTool(definition), Does.Contain("Created"));

        var result = await Tool(engine, "EchoText").InvokeAsync(new AIFunctionArguments { ["text"] = "a\"; rm -rf / #" });

        Assert.That(result?.ToString(), Is.EqualTo("echo:a\"; rm -rf / #"), "arguments must arrive as data, never as code");
    }

    [Test]
    public void ApprovedTool_PersistsAcrossSessions_PendingToolDoesNot()
    {
        var first = Engine();
        first.CreateTool(AddTool);
        first.CreateTool(AddTool.Replace("AddNumbers", "OtherTool"));

        Assert.That(first.HandleUserCommand("/approve AddNumbers"), Does.Contain("Approved 'AddNumbers'"));
        Assert.That(first.Tools.OfType<ScriptTool>().Single(t => t.Name == "AddNumbers").Approved, Is.True);

        var second = Engine();
        var names = second.Tools.Select(t => t.Name).ToList();
        Assert.That(names, Does.Contain("AddNumbers"));
        Assert.That(names, Does.Not.Contain("OtherTool"), "pending tools only live for the session that created them");
        Assert.That(second.ListAgentTools(), Does.Contain("OtherTool [pending from an earlier session, not loaded]"));

        // A pending tool from an earlier session can still be approved later.
        Assert.That(second.HandleUserCommand("/approve OtherTool"), Does.Contain("Approved"));
        Assert.That(second.Tools.Select(t => t.Name), Does.Contain("OtherTool"));
    }

    [Test]
    public void Reject_DeletesPendingToolAndRemovesItFromSession()
    {
        var engine = Engine();
        engine.CreateTool(AddTool);

        Assert.That(engine.HandleUserCommand("/reject AddNumbers"), Does.Contain("Rejected"));
        Assert.That(engine.Tools.Select(t => t.Name), Does.Not.Contain("AddNumbers"));
        Assert.That(File.Exists(Path.Combine(_skillsDir, "tools", "pending", "AddNumbers.tool.md")), Is.False);
    }

    [Test]
    public void ToolsChanged_FiresOnCreateApproveAndReject()
    {
        // Hosts that publish Tools (the MCP server) resync on this event.
        var engine = Engine();
        int changes = 0;
        engine.ToolsChanged += () => changes++;

        engine.CreateTool(AddTool);
        Assert.That(changes, Is.EqualTo(1), "create");

        engine.HandleUserCommand("/approve AddNumbers");
        Assert.That(changes, Is.EqualTo(2), "approve changes the description");

        engine.CreateTool(AddTool.Replace("AddNumbers", "OtherTool"));
        engine.HandleUserCommand("/reject OtherTool");
        Assert.That(changes, Is.EqualTo(4), "create + reject");
    }

    [Test]
    public void Skills_ReflectsReloadedSkillFiles()
    {
        var engine = Engine();
        Assert.That(engine.Skills.Select(s => s.Name), Does.Not.Contain("Greet"));

        engine.CreateSkillFile("greeting", "## Skill: Greet\n\n- Description: hi\n- Execution:\n\n```bash\necho hi\n```\n");

        Assert.That(engine.Skills.Single(s => s.Name == "Greet").Language, Is.EqualTo("bash"));
    }

    [Test]
    public void ModelCannotApproveTools()
    {
        var names = Engine().Tools.Select(t => t.Name).ToList();

        Assert.That(names, Does.Contain("CreateTool"));
        Assert.That(names.Where(n => n.Contains("Approve", StringComparison.OrdinalIgnoreCase) ||
                                     n.Contains("Reject", StringComparison.OrdinalIgnoreCase) ||
                                     n.Contains("UserCommand", StringComparison.OrdinalIgnoreCase)), Is.Empty);
    }

    [Test]
    public void HandleUserCommand_IgnoresNormalPrompts()
    {
        Assert.That(Engine().HandleUserCommand("please approve my PR"), Is.Null);
        Assert.That(Engine().HandleUserCommand("/unknown"), Is.Null);
    }

    [Test]
    [Category("Integration")]
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
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent(callId, name, arguments)]));
    }
}
