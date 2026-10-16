using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Core.Tests.Agent;

// F4 step 2: a byte-stable prefix. The prompt cache is keyed by the exact bytes of tools + system prompt, so two engines
// built from the same options must send the same tools in the same order, whatever order reflection returns methods in.
public class ToolOrderTests
{
    private const string ApprovedTool = """
        # Tool: AaaFirstByName
        - Description: Sorts before every built-in tool
        - Timeout: 30
        - Parameters:
          - `a` (integer, required): a number
        - Implementation:

        ```python
        print(1)
        ```
        """;

    private string _skillsDir = null!;

    [SetUp]
    public void SetUp()
    {
        _skillsDir = Directory.CreateTempSubdirectory("tool-order-").FullName;
        Directory.CreateDirectory(Path.Combine(_skillsDir, "agent"));
        File.WriteAllText(Path.Combine(_skillsDir, "agent", "default.md"), "# Default");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    private AgentEngine Engine()
    {
        var options = new AgentOptions { WorkspaceRoot = _skillsDir, SkillsDirectory = _skillsDir, AgentFunction = "louis", RatesUrl = null };
        var engine = new AgentEngine(AgentHost.LoadSkills(options), new FakeChatClient(), options) { SkillsDirectory = _skillsDir };
        engine.LoadApprovedTools();
        return engine;
    }

    /// <summary>What the provider is sent for the tools: each one's name, description and parameter schema, in order.</summary>
    private static string ToolsAsSent(AgentEngine engine) => JsonSerializer.Serialize(
        engine.Tools.OfType<AIFunction>().Select(t => new { t.Name, t.Description, Schema = t.JsonSchema.GetRawText() }));

    [Test]
    public void Tools_AreInNameOrder()
    {
        var names = Engine().Tools.Select(t => t.Name).ToList();

        Assert.That(names, Is.EqualTo(names.Order(StringComparer.Ordinal)));
        Assert.That(names, Is.Unique);
    }

    [Test]
    public void TwoEngines_SameOptions_SendTheSameToolsAndSystemPrompt()
    {
        AgentEngine first = Engine(), second = Engine();

        Assert.That(ToolsAsSent(second), Is.EqualTo(ToolsAsSent(first)));
        Assert.That(second.SkillDocumentation, Is.EqualTo(first.SkillDocumentation));
    }

    [Test]
    public void TwoToolsWithOneName_Throw()
    {
        // A toolset whose method has the name of a built-in tool: the model couldn't tell them apart.
        var options = new AgentOptions { WorkspaceRoot = _skillsDir, SkillsDirectory = _skillsDir, RatesUrl = null };

        Assert.That(() => new AgentEngine(AgentHost.LoadSkills(options), new FakeChatClient(), options, toolsets: [new Clash(), new Clash()]),
            Throws.InvalidOperationException.With.Message.Contain("Two tools are named 'Ping'"));
    }

    private sealed class Clash
    {
        [System.ComponentModel.Description("Answers pong")]
        public string Ping() => "pong";
    }

    [Test]
    public void AnAgentBuiltTool_TakesItsPlaceInNameOrder()
    {
        // It sorts first, so it lands at the start, not appended at the end.
        Directory.CreateDirectory(Path.Combine(_skillsDir, "tools"));
        File.WriteAllText(Path.Combine(_skillsDir, "tools", "AaaFirstByName.tool.md"), ApprovedTool);

        var names = Engine().Tools.Select(t => t.Name).ToList();

        Assert.That(names[0], Is.EqualTo("AaaFirstByName"));
        Assert.That(names, Is.EqualTo(names.Order(StringComparer.Ordinal)));
    }
}