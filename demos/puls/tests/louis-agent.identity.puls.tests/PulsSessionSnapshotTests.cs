using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Skills;
using LouisAgent.Packs.Recipes;
using LouisAgent.Packs.ShopPrices;
using LouisAgent.Packs.WhereToBuy;
using Microsoft.Extensions.AI;

namespace LouisAgent.Identities.Puls.Tests;

/// <summary>
/// Pins what the model is sent in a Puls session: the chat (puls/voice-af with specialists and web, as the web app
/// opens it) and the cook (puls-cook with web and what its header declares, as Puls hands it the work: the recipes,
/// where to buy and the shop prices). Core's golden never saw a Puls session, so this one is the proof that the
/// identity keeps what it sends. A deliberate change regenerates it with LOUIS_UPDATE_GOLDEN=1: run, review the diff,
/// commit.
/// </summary>
public class PulsSessionSnapshotTests
{
    private static readonly string GoldenFile =
        Path.Combine(TestPaths.RepoRoot, "demos/puls/tests/louis-agent.identity.puls.tests", "PulsSessionSnapshot.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private string _skillsDir = null!;

    /// <summary>Gives each test a folder of its own for the skills, so no test sees another's files.</summary>
    [SetUp]
    public void SetUp()
    {
        _skillsDir = Directory.CreateTempSubdirectory("puls-session-").FullName;
    }

    /// <summary>Removes the folder the test wrote its skills into.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    /// <summary>
    /// The engine factory with the Puls stack's settings and packs, over a fake model that is never called.
    /// </summary>
    private AgentEngineFactory Factory()
    {
        // The Puls stack's settings, as far as building an engine reads them: a peer for the specialists toolset, and a
        // browser (nothing is connected while an engine is built).
        var options = new AgentOptions
        {
            WorkspaceRoot = _skillsDir,
            SkillsDirectory = _skillsDir,
            RatesUrl = null,
            AgentPeers = new Dictionary<string, Uri> { [SampleIdentity.Alpha] = new("http://puls-agent.test/") },
            BrowserUrl = "http://browser.test:9222",
        };
        // Where to buy and shop prices are packs Puls names (F50.2); the catalogue is what IDENTITIES=puls builds.
        // The recipes pack is the other one Puls names, with every site on as the deployments have it
        // (RECIPE_SOURCES=*), because the cook's header asks for its toolset.
        var catalog = new AgentCatalog([new PulsIdentity()],
        [
            new WhereToBuyPack(), new ShopPricesPack(new ShopPricesOptions()),
            new RecipesPack(new RecipesOptions { RecipeSources = "*" })
        ]);
        return new AgentEngineFactory(new FakeChatClient(), options, catalog: catalog);
    }

    /// <summary>What the model is sent from an engine: its tools with their schemas, and its system prompt.</summary>
    private static object View(AgentEngine engine) => new
    {
        Tools = engine.Tools.OfType<AIFunction>()
            .Select(t => new
            {
                t.Name,
                t.Description,
                Schema = JsonDocument.Parse(t.JsonSchema.GetRawText()).RootElement
            }),
        SystemPrompt = engine.SkillDocumentation,
    };

    /// <summary>
    /// The toolsets the cook's session opens with, as Puls hands it the work: <c>web</c> from Puls, plus whatever the
    /// cook's own header declares (ADR 0025), so a change to the header changes the golden and no list is typed here.
    /// </summary>
    private static IEnumerable<string> CookToolsets() =>
        ToolsetCatalog.Normalise([
            CoreToolset.Web.Wire(), .. SkillLoader.ToolsetsOf(PulsRole.Cook.Wire(),
                RoleLevel.Specialist)
        ]);

    /// <summary>
    /// Both sessions as one JSON text, with this machine's temporary folder replaced so the file is the same
    /// everywhere.
    /// </summary>
    private string Snapshot()
    {
        var factory = Factory();
        var snapshot = new
        {
            Chat = View(factory.GetOrCreate([CoreToolset.Specialists.Wire(), CoreToolset.Web.Wire()], "puls/voice-af",
                RoleLevel.Agent)),
            Cook = View(factory.GetOrCreate(CookToolsets(), PulsRole.Cook.Wire(), RoleLevel.Specialist)),
        };
        return JsonSerializer.Serialize(snapshot, Json)
            .Replace(JsonEncodedText.Encode(_skillsDir).ToString(), "<skills>")
            .Replace(_skillsDir, "<skills>")
            .ReplaceLineEndings("\n") + "\n";
    }

    /// <summary>The snapshot matches the golden file, or with LOUIS_UPDATE_GOLDEN=1 rewrites it.</summary>
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
            "What a Puls session's model is sent changed. If that is intended, rerun with " +
            "LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }

    /// <summary>Building the snapshot again, in a fresh folder, gives the same text.</summary>
    [Test]
    public void TheSnapshot_IsTheSameOnEveryBuild()
    {
        string first = Snapshot();
        TearDown();
        SetUp();

        Assert.That(Snapshot(), Is.EqualTo(first));
    }
}
