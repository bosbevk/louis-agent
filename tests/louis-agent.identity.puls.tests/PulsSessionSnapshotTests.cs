using LouisAgent.Core.Agent.Catalog;
using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Packs.Shops;
using Microsoft.Extensions.AI;

namespace LouisAgent.Identities.Puls.Tests;

// CL15 step 0: what the model is sent in a Puls session must not change while Puls and the shops leave core, except
// where a step says it does. The coding agent's golden (ModelRequestSnapshotTests) never pinned a Puls session, so the
// cut had no proof; this one pins the two sessions Puls runs: the chat (puls/voice-af with specialists and web, as the
// web app opens it) and the cook (puls-cook with web and shops, as Puls hands it the work). Written from main before
// the first move, in core's tests; here, with Puls's identity, from step 2. A deliberate change regenerates it: LOUIS_UPDATE_GOLDEN=1, run, review the diff, commit.
public class PulsSessionSnapshotTests
{
    private static readonly string GoldenFile =
        Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.identity.puls.tests", "PulsSessionSnapshot.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private string _skillsDir = null!;

    [SetUp]
    public void SetUp()
    {
        _skillsDir = Directory.CreateTempSubdirectory("puls-session-").FullName;
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    private AgentEngineFactory Factory()
    {
        // The Puls stack's settings, as far as building an engine reads them: a peer for the specialists toolset, and a
        // shop with a browser for the shops toolset (F33; nothing is connected while an engine is built).
        var options = new AgentOptions
        {
            WorkspaceRoot = _skillsDir, SkillsDirectory = _skillsDir, RatesUrl = null,
            AgentPeers = new Dictionary<string, Uri> { ["puls"] = new("http://puls-agent.test/") },
            BrowserUrl = "http://browser.test:9222",
        };
        // CL15 step 4: the shops are the pack Puls names, with its own settings; the catalogue is what IDENTITIES=puls builds.
        var catalog = new AgentCatalog([new PulsIdentity()], [new ShopsPack(new ShopsOptions { ShopCatalogues = "checkers" })]);
        return new AgentEngineFactory(new FakeChatClient(), options, catalog: catalog);
    }

    private static object View(AgentEngine engine) => new
    {
        Tools = engine.Tools.OfType<AIFunction>()
            .Select(t => new { t.Name, t.Description, Schema = JsonDocument.Parse(t.JsonSchema.GetRawText()).RootElement }),
        SystemPrompt = engine.SkillDocumentation,
    };

    /// <summary>Both sessions, with this machine's temporary folder replaced so the file is the same everywhere.</summary>
    private string Snapshot()
    {
        var factory = Factory();
        var snapshot = new
        {
            Chat = View(factory.GetOrCreate(["specialists", "web"], "puls/voice-af", RoleLevel.Agent)),
            Cook = View(factory.GetOrCreate(["web", "shops"], "puls-cook", RoleLevel.Specialist)),
        };
        return JsonSerializer.Serialize(snapshot, Json)
            .Replace(JsonEncodedText.Encode(_skillsDir).ToString(), "<skills>")
            .Replace(_skillsDir, "<skills>")
            .ReplaceLineEndings("\n") + "\n";
    }

    [Test]
    public void WhatTheModelSees_InAPulsSession_MatchesTheGoldenFile()
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
            "What a Puls session's model is sent changed. If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }

    [Test]
    public void TheSnapshot_IsTheSameOnEveryBuild()
    {
        string first = Snapshot();
        TearDown();
        SetUp();

        Assert.That(Snapshot(), Is.EqualTo(first));
    }
}
