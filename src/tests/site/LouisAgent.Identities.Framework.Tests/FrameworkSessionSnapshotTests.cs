using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Packs.Docs;
using LouisAgent.Packs.Site;
using Microsoft.Extensions.AI;

namespace LouisAgent.Identities.Framework.Tests;

/// <summary>
/// Pins what the model is sent in a session of the framework Orbi (F164.2): its tools with their schemas and its
/// system prompt, as a visitor at the door opens it, in its own voice on its own two packs. A deliberate change
/// regenerates it with LOUIS_UPDATE_GOLDEN=1: run, review the diff, commit.
/// </summary>
public class FrameworkSessionSnapshotTests
{
    /// <summary>Where the golden file is.</summary>
    private static readonly string GoldenFile = Path.Combine(TestPaths.RepoRoot,
        "src/tests/site/LouisAgent.Identities.Framework.Tests", "FrameworkSessionSnapshot.json");

    /// <summary>Indented, with its own letters and signs, so a diff of it can be read.</summary>
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>One tool as the model is offered it.</summary>
    private sealed record ToolView(string Name, string Description, JsonElement Schema);

    /// <summary>What the model is sent: its tools and its system prompt.</summary>
    private sealed record SessionView(IReadOnlyList<ToolView> Tools, string SystemPrompt);

    private string _skillsDir = null!;

    /// <summary>Gives each test a folder of its own for the skills, so no test sees another's files.</summary>
    [SetUp]
    public void SetUp() => _skillsDir = Directory.CreateTempSubdirectory("framework-session-").FullName;

    /// <summary>Removes the folder the test wrote its skills into.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    /// <summary>
    /// The session a visitor at the door opens: the framework Orbi's voice, on the toolsets of the packs it names, over
    /// a fake model that is never called.
    /// </summary>
    private SessionView Session()
    {
        var orbi = new FrameworkIdentity();
        var catalog = new AgentCatalog([orbi], [new DocsPack(new DocsOptions { Root = "test-docs" }), new SitePack()]);
        var options = new AgentOptions
        {
            WorkspaceRoot = _skillsDir,
            SkillsDirectory = _skillsDir,
            RatesUrl = null
        };
        var factory = new AgentEngineFactory(new FakeChatClient(), options, catalog: catalog);

        var engine = factory.GetOrCreate(catalog.ToolsetsOf(orbi), orbi.DefaultVoice);
        var tools = engine.Tools.OfType<AIFunction>()
            .Select(tool => new ToolView(tool.Name, tool.Description, tool.JsonSchema)).ToList();
        return new SessionView(tools, engine.SkillDocumentation);
    }

    /// <summary>
    /// The session as JSON, with this machine's temporary folder replaced so the file is the same everywhere.
    /// </summary>
    private string Snapshot() => JsonSerializer.Serialize(Session(), Json)
        .Replace(JsonEncodedText.Encode(_skillsDir).ToString(), "<skills>")
        .Replace(_skillsDir, "<skills>")
        .ReplaceLineEndings("\n") + "\n";

    /// <summary>The snapshot matches the golden file, or with LOUIS_UPDATE_GOLDEN=1 rewrites it.</summary>
    [Test]
    public void WhatTheModelSees_InAFrameworkOrbiSession_MatchesTheGoldenFile()
    {
        string actual = Snapshot();
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            File.WriteAllText(GoldenFile, actual);
            Assert.Pass($"Wrote {GoldenFile}; review its diff before committing.");
        }

        Assert.That(File.Exists(GoldenFile), $"No golden file; run with LOUIS_UPDATE_GOLDEN=1 to write {GoldenFile}.");
        string expected = File.ReadAllText(GoldenFile).ReplaceLineEndings("\n");
        Assert.That(actual, Is.EqualTo(expected),
            "What a framework Orbi session's model is sent changed. If that is intended, rerun with " +
            "LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }
}
