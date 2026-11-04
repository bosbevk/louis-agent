using LouisAgent.Choices;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Agent.Toolsets;
using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Packs.DevOps;
using LouisAgent.Packs.Git;
using LouisAgent.Packs.DotNet;
using LouisAgent.Packs.Paymo;
using Microsoft.Extensions.AI;

namespace LouisAgent.Coding.Tests.Golden;

// CL2: what the model is sent must not change while AgentEngine is split. The tools (names, descriptions, schemas, in
// order), the system prompt and the engine's fixed texts are compared with a committed golden file. A deliberate change
// regenerates it: set LOUIS_UPDATE_GOLDEN=1, run this test, review the file's diff, commit it.
public class ModelRequestSnapshotTests
{
    private static readonly string GoldenFile =
        Path.Combine(TestPaths.RepoRoot, "demos/louis-agent/tests/louis-agent.coding-catalog.tests/Golden", "ModelRequestSnapshot.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>What this demo loads, with keys that are never used: nothing is called while building the engine.</summary>
    private static readonly AgentCatalog Catalog = new([],
        CodingCatalog.Packs(new PaymoOptions("snapshot"), new DevOpsOptions("snapshot", "org", "project")));

    /// <summary>The order of the golden file, whichever order the catalogue lists them in.</summary>
    private static readonly string[] Order =
    [
        CoreToolset.Files.Wire(), GitToolset.Git.Wire(), DotNetToolset.DotNet.Wire(), CoreToolset.Python.Wire(),
        CoreToolset.PowerShell.Wire(), CoreToolset.Bash.Wire(), CoreToolset.Web.Wire(), PaymoToolset.Paymo.Wire(),
        DevOpsToolset.DevOps.Wire(), CoreToolset.Specialists.Wire(), CoreToolset.Minions.Wire(),
        CoreToolset.Skills.Wire(),
    ];

    private string _skillsDir = null!;

    [OneTimeSetUp]
    public void UseThePacksPrompts() => SkillFileSource.Use(Catalog.Prompts);

    [SetUp]
    public void SetUp()
    {
        _skillsDir = Directory.CreateTempSubdirectory("model-request-").FullName;
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    private AgentEngine Engine(bool supportsTools = true)
    {
        // The settings every toolset needs, so all of them load.
        var options = new AgentOptions
        {
            WorkspaceRoot = _skillsDir,
            SkillsDirectory = _skillsDir,
            RatesUrl = null,
            AgentPeers = new Dictionary<string, Uri> { ["coder"] = new("http://coder.test/") },
            // F30: the minions toolset needs a model map too; the snapshot then holds StartMinion and ContinueMinion.
            MinionLlms = new Dictionary<string, IReadOnlyList<string>> { ["*"] = ["instance"] },
            // F33: a browser for the web toolset's renderer; building the engine connects to nothing. (The shops are a pack, CL15.)
            BrowserUrl = "http://browser.test:9222",
        };
        // The snapshot names every toolset (F5: an engine has only what it's given), in the order the file holds them.
        var context = new ToolsetContext(options, WebSearchProviderFactory.Create(options));
        var resolved = Catalog.Resolve(Order, context);
        var toolsets = Order.Select(name => resolved.Single(t => t.Name == name)).ToList();
        return new AgentEngine(SkillLoader.Load(options, toolsets), new FakeChatClient(), options, supportsTools,
            toolsets: ToolsetCatalog.BuildTools(toolsets, context), selfExtension: true)
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
            TurnRunner.ContinueAfterCutOffMessage,
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
