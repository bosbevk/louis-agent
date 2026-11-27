using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>
/// F4 step 2: a byte-stable prefix. The prompt cache is keyed by the exact bytes of the tools and the system prompt, so
/// two engines built from the same options must send the same tools in the same order, whatever order reflection
/// returns methods in.
/// </summary>
public class ToolOrderTests
{
    private string _skillsDir = null!;

    /// <summary>A skills folder of its own for each test.</summary>
    [SetUp]
    public void SetUp() => _skillsDir = Directory.CreateTempSubdirectory("tool-order-").FullName;

    /// <summary>The folder goes with the test.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    /// <summary>The tools are in name order, each name once.</summary>
    [Test]
    public void Tools_AreInNameOrder()
    {
        var names = Engine().Tools.Select(t => t.Name).ToList();

        Assert.That(names, Is.EqualTo(names.Order(StringComparer.Ordinal)));
        Assert.That(names, Is.Unique);
    }

    /// <summary>
    /// Two engines of the same options send the same tools and the same system prompt, byte for byte.
    /// </summary>
    [Test]
    public void TwoEngines_SameOptions_SendTheSameToolsAndSystemPrompt()
    {
        AgentEngine first = Engine(), second = Engine();

        Assert.That(ToolsAsSent(second), Is.EqualTo(ToolsAsSent(first)));
        Assert.That(second.SkillDocumentation, Is.EqualTo(first.SkillDocumentation));
    }

    /// <summary>
    /// A toolset whose method has the name of another tool is refused: the model couldn't tell them apart.
    /// </summary>
    [Test]
    public void TwoToolsWithOneName_Throw()
    {
        var options = Options();

        Assert.That(
            () => new AgentEngine(SkillLoader.Load(options), new FakeChatClient(), options,
                toolsets: [new Clash(), new Clash()]),
            Throws.InvalidOperationException.With.Message.Contain("Two tools are named 'Ping'"));
    }

    /// <summary>The engine's settings, over this test's folder.</summary>
    private AgentOptions Options() =>
        new() { WorkspaceRoot = _skillsDir, SkillsDirectory = _skillsDir, RatesUrl = null };

    /// <summary>An engine over this test's folder.</summary>
    private AgentEngine Engine()
    {
        var options = Options();
        return new AgentEngine(SkillLoader.Load(options), new FakeChatClient(), options)
        {
            SkillsDirectory = _skillsDir
        };
    }

    /// <summary>What the provider is sent for the tools: each one's name, description and schema, in order.</summary>
    private static string ToolsAsSent(AgentEngine engine) => JsonSerializer.Serialize(
        engine.Tools.OfType<AIFunction>()
            .Select(t => new ToolAsSent(t.Name, t.Description, t.JsonSchema.GetRawText())));

    /// <summary>One tool as the provider is sent it.</summary>
    private sealed record ToolAsSent(string Name, string Description, string Schema);

    /// <summary>A toolset whose one tool has a name another toolset uses too.</summary>
    private sealed class Clash
    {
        /// <summary>Answers pong.</summary>
        [System.ComponentModel.Description("Answers pong")]
        public string Ping() => "pong";
    }
}
