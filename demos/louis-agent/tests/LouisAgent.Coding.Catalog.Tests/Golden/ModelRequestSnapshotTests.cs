using LouisAgent.Core.Web;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Agent.Toolsets;
using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Web.Search;
using Microsoft.Extensions.AI;

namespace LouisAgent.Coding.Tests.Golden;

/// <summary>
/// What the model is sent never changes by accident. The tools (names, descriptions, schemas, in order), the system
/// prompt and the engine's fixed texts are compared with a committed golden file. A deliberate change regenerates it:
/// set LOUIS_UPDATE_GOLDEN=1, run this test, review the file's diff, commit it.
/// </summary>
public class ModelRequestSnapshotTests
{
    private static readonly string GoldenFile =
        Path.Combine(TestPaths.RepoRoot, "demos/louis-agent/tests/LouisAgent.Coding.Catalog.Tests/Golden",
            "ModelRequestSnapshot.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private string _skillsDir = null!;


    /// <summary>Each test gets an empty skills folder of its own.</summary>
    [SetUp]
    public void SetUp()
    {
        _skillsDir = Directory.CreateTempSubdirectory("model-request-").FullName;
    }

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    /// <summary>The coding agent's engine with every toolset loaded, with or without tool support.</summary>
    private AgentEngine Engine(bool supportsTools = true)
    {
        // The settings every toolset needs, so all of them load.
        var options = new AgentOptions
        {
            WorkspaceRoot = _skillsDir,
            DefaultVoice = CodingPrompts.Catalog.DefaultVoice,
            SkillsDirectory = _skillsDir,
            RatesUrl = null,
            // The agents toolset needs a peer; AskAgent asks lookup agents too (F127.5), so no model map is needed for
            // it.
            AgentPeers = new Dictionary<string, Uri> { ["coder"] = new("http://coder.test/") },
            // F33: a browser for the web toolset's renderer; building the engine connects to nothing. (The shops are
            // a pack, CL15.)
            BrowserUrl = "http://browser.test:9222",
        };
        // Every toolset (F5: an engine has only what it's given), in the catalogue's order, which is the file's.
        var context = new ToolsetContext(options, WebAccess.Create(options));
        var toolsets = CodingPrompts.Catalog.Resolve(CodingPrompts.Catalog.Toolsets.Select(t => t.Name), context);
        return new AgentEngine(SkillLoader.Load(options, toolsets), new FakeChatClient(), options, supportsTools,
            toolsets: ToolsetCatalog.BuildTools(toolsets, context), selfExtension: true)
        {
            SkillsDirectory = _skillsDir,
        };
    }

    /// <summary>
    /// What the model sees, with this machine's temporary folder replaced so the file is the same everywhere.
    /// </summary>
    private string Snapshot()
    {
        AgentEngine engine = Engine();
        var snapshot = new
        {
            Tools = engine.Tools.OfType<AIFunction>()
                .Select(t => new
                {
                    t.Name,
                    t.Description,
                    Schema = JsonDocument.Parse(t.JsonSchema.GetRawText()).RootElement
                }),
            SystemPrompt = engine.SkillDocumentation,
            NoToolsHistory = Engine(supportsTools: false).StartConversation().Messages.Select(m => new
            {
                Role = m.Role.Value,
                m.Text
            }),
            ToolCallGuards.TruncatedCallMessage,
            TurnRunner.ContinueAfterCutOffMessage,
        };
        return JsonSerializer.Serialize(snapshot, Json)
            .Replace(JsonEncodedText.Encode(_skillsDir).ToString(), "<skills>")
            .Replace(_skillsDir, "<skills>")
            .ReplaceLineEndings("\n") + "\n";
    }

    /// <summary>What the model is sent matches the golden file.</summary>
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
            "What the model is sent changed. If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the " +
            "diff.");
    }

    /// <summary>
    /// Two engines in two folders give the same snapshot, so nothing in it depends on the machine or run.
    /// </summary>
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
