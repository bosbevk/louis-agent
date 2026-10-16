using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent;

// CL2: what the model is sent must not change while AgentEngine is split. The tools (names, descriptions, schemas, in
// order), the system prompt and the engine's fixed texts are compared with a committed golden file. A deliberate change
// regenerates it: set LOUIS_UPDATE_GOLDEN=1, run this test, review the file's diff, commit it.
public class ModelRequestSnapshotTests
{
    private static readonly string GoldenFile =
        Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.core.tests", "Agent", "ModelRequestSnapshot.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private string _skillsDir = null!;

    [SetUp]
    public void SetUp()
    {
        _skillsDir = Directory.CreateTempSubdirectory("model-request-").FullName;
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    private AgentEngine Engine(bool supportsTools = true)
    {
        // Every toolset: Paymo and DevOps load only with their keys (nothing is called while building the engine).
        var options = new AgentOptions
        {
            WorkspaceRoot = _skillsDir, SkillsDirectory = _skillsDir, AgentFunction = "louis", RatesUrl = null,
            PaymoApiKey = "snapshot", DevopsApiKey = "snapshot", DevopsOrganization = "org", DevopsProject = "project",
            AgentPeers = new Dictionary<string, Uri> { ["coder"] = new("http://coder.test/") },
        };
        // F5: an engine has only the toolsets it is given, so the snapshot names them all (and self-extension).
        var toolsets = TestPaths.Toolsets(options, ToolsetCatalog.All.Select(t => t.Name));
        return new AgentEngine(AgentHost.LoadSkills(options), new FakeChatClient(), options, supportsTools, toolsets: toolsets, selfExtension: true)
        {
            SkillsDirectory = _skillsDir,
        };
    }

    /// <summary>What the model sees, with this machine's temporary folder replaced so the file is the same everywhere.</summary>
    private string Snapshot()
    {
        AgentEngine engine = Engine();
        var snapshot = new
        {
            Tools = engine.Tools.OfType<AIFunction>()
                .Select(t => new { t.Name, t.Description, Schema = JsonDocument.Parse(t.JsonSchema.GetRawText()).RootElement }),
            SystemPrompt = engine.SkillDocumentation,
            NoToolsHistory = Engine(supportsTools: false).NewHistory().Select(m => new { Role = m.Role.Value, m.Text }),
            ToolCallGuards.TruncatedCallMessage,
            AgentEngine.ContinueAfterCutOffMessage,
        };
        return JsonSerializer.Serialize(snapshot, Json)
            .Replace(JsonEncodedText.Encode(_skillsDir).ToString(), "<skills>")
            .Replace(_skillsDir, "<skills>")
            .ReplaceLineEndings("\n") + "\n";
    }

    [Test]
    public void WhatTheModelSees_MatchesTheGoldenFile()
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
            "What the model is sent changed. If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }

    [Test]
    public void TheSnapshot_IsTheSameOnEveryBuild()
    {
        // Two engines in two temporary folders give one snapshot: nothing machine- or run-specific is left in it.
        string first = Snapshot();
        TearDown();
        SetUp();

        Assert.That(Snapshot(), Is.EqualTo(first));
    }
}