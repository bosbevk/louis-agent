using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Replay;
using LouisAgent.Core.Agent.Tools;
using LouisAgent.Testing.Replay;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.Tools;

/// <summary>
/// A registry that keeps recordings (FX235) holds each tool recorded around, and still takes one out by the tool it
/// was given, as an agent-built tool is replaced.
/// </summary>
public class ToolRegistryReplayTests
{
    /// <summary>A fresh replay folder for each test.</summary>
    private ReplayFolder _folder = null!;

    /// <summary>Makes the test's replay folder.</summary>
    [SetUp]
    public void SetUp() => _folder = new ReplayFolder();

    /// <summary>Deletes the test's replay folder.</summary>
    [TearDown]
    public void TearDown() => _folder.Dispose();

    /// <summary>A tool that answers "ok", named so.</summary>
    private static AIFunction Tool(string name) => AIFunctionFactory.Create(() => "ok", name);

    /// <summary>Each tool comes in recorded around, by its own name; without a store it comes in as itself.</summary>
    [Test]
    public void WithAStore_EveryToolIsRecordedAround()
    {
        var recording = new ToolRegistry(replay: _folder.Store);
        var plain = new ToolRegistry();
        AIFunction search = Tool("SearchDocs");

        recording.Add(search);
        plain.Add(search);

        Assert.Multiple(() =>
        {
            Assert.That(recording.Live.Single(), Is.InstanceOf<RecordedTool>());
            Assert.That(recording.Live.Single().Name, Is.EqualTo("SearchDocs"));
            Assert.That(plain.Live.Single(), Is.SameAs(search));
        });
    }

    /// <summary>The tool given is taken out again, though the list holds the recording around it.</summary>
    [Test]
    public void ARecordedTool_IsTakenOutByTheToolGiven()
    {
        var registry = new ToolRegistry(replay: _folder.Store);
        AIFunction script = Tool("AddNumbers");
        registry.Add(script);
        registry.Add(Tool("SearchDocs"));

        bool removed = registry.Remove(script);

        Assert.That(removed, Is.True);
        Assert.That(registry.Live.Select(t => t.Name), Is.EqualTo(new[] { "SearchDocs" }));
    }
}
