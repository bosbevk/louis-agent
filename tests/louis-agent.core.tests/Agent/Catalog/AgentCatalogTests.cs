using LouisAgent.Choices;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Agent.EngineTools;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Core.Tests.Agent.Catalog;

/// <summary>
/// Checks the one catalogue a host builds from the identities and packs it was given: it is checked as it is built
/// and passed down, and core's own toolsets stay in <c>ToolsetCatalog</c>. Every test builds its own catalogue from a
/// fake identity and a fake pack, so there is no global state.
/// </summary>
public class AgentCatalogTests
{
    /// <summary>A toolset context from a real engine factory, for resolving toolsets.</summary>
    private static ToolsetContext Context() => new AgentEngineFactory(new FakeChatClient(), new AgentOptions
    {
        WorkspaceRoot = TestPaths.RepoRoot,
        SkillsDirectory = Path.Combine(TestPaths.RepoRoot, "Skills"),
        RatesUrl = null,
    }).Context;

    /// <summary>A catalogue of the identities and packs given, none if left out.</summary>
    private static AgentCatalog Catalog(IReadOnlyList<IAgentIdentity>? identities = null,
        IReadOnlyList<IAgentPack>? packs = null) =>
        new(identities ?? [], packs ?? []);

    /// <summary>A pack's toolset is found beside core's, in the catalogue's order.</summary>
    [Test]
    public void APacksToolset_ResolvesThroughTheCatalogue_BesideCores()
    {
        var catalog = Catalog([new FakeIdentity(packs: ["fakeshop"])], [new FakePack()]);

        Assert.That(catalog.Toolsets.Select(t => t.Name), Does.Contain("fakeshop").And.Contain(CoreToolset.Web.Wire()),
            "core's and the pack's");
        Assert.That(catalog.Toolsets.Take(ToolsetCatalog.All.Count).Select(t => t.Name),
            Is.EqualTo(ToolsetCatalog.All.Select(t => t.Name)), "core's first, in order");
        Assert.That(catalog.Resolve(["fakeshop", CoreToolset.Web.Wire()], Context()).Select(t => t.Name),
            Is.EqualTo(new[] { CoreToolset.Web.Wire(), "fakeshop" }), "resolved in catalogue order");
        Assert.That(catalog.Identities.Single().Name, Is.EqualTo("fakeid"));
        Assert.That(catalog.Packs.Single().Name, Is.EqualTo("fakeshop"));
        Assert.That(catalog.Prompts, Has.Count.EqualTo(2), "the identity's and the pack's, for the prompt reader");
    }

    /// <summary>Two identities that name the same pack get one pack, not two.</summary>
    [Test]
    public void TwoIdentities_NamingOnePack_ShareIt()
    {
        var shops = new FakePack();
        var catalog = Catalog([new FakeIdentity("one", ["fakeshop"]), new FakeIdentity("two", ["fakeshop"])], [shops]);

        Assert.That(catalog.Packs, Is.EqualTo(new[] { shops }));
        Assert.That(catalog.Toolsets.Count(t => t.Name == "fakeshop"), Is.EqualTo(1));
    }

    /// <summary>Two packs with the same name can't both be registered.</summary>
    [Test]
    public void APackNamedTwice_IsRefused()
    {
        var error = Assert.Throws<AgentCatalogException>(() => Catalog(packs:
            [new FakePack("same"), new FakePack("same")]))!;

        Assert.That(error.Message, Does.Contain("same").And.Contain("pack"));
    }

    /// <summary>
    /// The packs' feeds are gathered in the packs' order and found by name, case ignored; a pack that publishes none
    /// adds none (ADR 0070).
    /// </summary>
    [Test]
    public void ThePacksFeeds_AreGathered_AndFoundByName()
    {
        var catalog = Catalog(packs:
        [
            new FakePack("first", toolsets: [], feeds: [new FakeFeed("prices"), new FakeFeed("rents")]),
            new FakePack("second", toolsets: []),
            new FakePack("third", toolsets: [], feeds: [new FakeFeed("fuel")])
        ]);

        Assert.That(catalog.Feeds.Select(f => f.Name), Is.EqualTo(new[] { "prices", "rents", "fuel" }));
        Assert.That(catalog.Feed("RENTS")?.Name, Is.EqualTo("rents"));
        Assert.That(catalog.Feed("nope"), Is.Null);
        Assert.That(AgentCatalog.CoreOnly.Feeds, Is.Empty);
    }

    /// <summary>Two feeds with one name, in one pack or two, can't both be registered: a feed has one URL.</summary>
    [Test]
    public void AFeedNamedTwice_IsRefused()
    {
        var error = Assert.Throws<AgentCatalogException>(() => Catalog(packs:
            [
                new FakePack("first", toolsets: [], feeds: [new FakeFeed("prices")]),
                new FakePack("second", toolsets: [], feeds: [new FakeFeed("Prices")])
            ]))!;

        Assert.That(error.Message, Does.Contain("Prices").And.Contain("feed"));
    }

    /// <summary>A pack can't take the name of one of core's toolsets.</summary>
    [Test]
    public void AToolsetNamedLikeCores_IsRefused()
    {
        var pack = new FakePack("mine",
            [FakePack.Toolset(CoreToolset.Web.Wire(), typeof(FakeShopTools), _ => [new FakeShopTools()])]);

        var error = Assert.Throws<AgentCatalogException>(() => Catalog(packs: [pack]))!;

        Assert.That(error.Message, Does.Contain(CoreToolset.Web.Wire()).And.Contain("toolset"));
    }

    /// <summary>The same prompt file in two places is refused, so the reader never has to choose.</summary>
    [Test]
    public void APromptPathInTwoPlaces_IsRefused()
    {
        // A pack that ships core's own web guide, and two identities with the same mode file.
        var pack = new FakePack("mine", prompts: new Dictionary<string, string> { ["toolsets/web.md"] = "# web" });
        Assert.That(Assert.Throws<AgentCatalogException>(() => Catalog(packs: [pack]))!.Message,
            Does.Contain("toolsets/web.md"));

        var voice = new Dictionary<string, string> { ["agent/shared/voice.md"] = "# shared" };
        var error = Assert.Throws<AgentCatalogException>(() =>
            Catalog([new FakeIdentity("one", prompts: voice), new FakeIdentity("two", prompts: voice)]))!;
        Assert.That(error.Message, Does.Contain("agent/shared/voice.md"));
    }

    /// <summary>A pack an identity or a pack asks for that nobody registered is refused, naming what is.</summary>
    [Test]
    public void ANameThatResolvesToNothing_IsRefused_ListingTheRegisteredOnes()
    {
        var error = Assert.Throws<AgentCatalogException>(() =>
            Catalog([new FakeIdentity(packs: ["nothing"])], [new FakePack()]))!;
        Assert.That(error.Message, Does.Contain("nothing").And.Contain("fakeid").And.Contain("fakeshop"));

        var required = Assert.Throws<AgentCatalogException>(() => Catalog(packs:
            [new FakePack("cooking", requires: ["pantry"])]))!;
        Assert.That(required.Message, Does.Contain("pantry").And.Contain("cooking"));
    }

    /// <summary>Packs that need each other in a circle are refused.</summary>
    [Test]
    public void ARequiresCycle_IsRefused()
    {
        var a = new FakePack("a", [FakePack.Toolset("a", typeof(FakeShopTools), _ => [])], requires: ["b"]);
        var b = new FakePack("b", [], requires: ["a"]);

        var error = Assert.Throws<AgentCatalogException>(() => Catalog(packs: [a, b]))!;

        Assert.That(error.Message, Does.Contain("cycle").And.Contain("a").And.Contain("b"));
    }

    /// <summary>A tool name two toolsets share, or one the engine already has, stops start-up.</summary>
    [Test]
    public void AToolNameInTwoToolsets_OrTheEngines_IsRefusedAtStartUp()
    {
        var clashing = new FakePack("mine",
            [FakePack.Toolset("mine", typeof(FakeClashingTools), _ => [new FakeClashingTools()])]);
        var twice = Assert.Throws<AgentCatalogException>(() => Catalog(packs: [clashing]))!;
        Assert.That(twice.Message, Does.Contain("FetchUrl").And.Contain(CoreToolset.Web.Wire()).And.Contain("mine"),
            "both toolsets named");

        var engines = new FakePack("clock",
            [FakePack.Toolset("clock", typeof(FakeEngineNameTools), _ => [new FakeEngineNameTools()])]);
        var taken = Assert.Throws<AgentCatalogException>(() => Catalog(packs: [engines]))!;
        Assert.That(taken.Message, Does.Contain("GetToday").And.Contain("engine"), "named as the engine's own");
    }

    /// <summary>The packs' tools count as taken, so a tool the agent builds can't reuse one's name.</summary>
    [Test]
    public void ToolNames_CountThePacksTools_SoAnAgentBuiltToolCantTakeOne()
    {
        var catalog = Catalog(packs: [new FakePack()]);

        Assert.That(catalog.ToolNames, Does.Contain("FindFakeProducts").And.Contain("FetchUrl"));
        Assert.That(catalog.ToolsetOf("FindFakeProducts"), Is.EqualTo("fakeshop"));
        Assert.That(catalog.ToolsetOf("FetchUrl"), Is.EqualTo(CoreToolset.Web.Wire()));
        Assert.That(catalog.ToolsetOf("CreateTool"), Is.Null, "engine-provided: no toolset builds it");
        Assert.That(catalog.ToolNamesOf(catalog.Resolve(["fakeshop"], Context())),
            Is.EqualTo(new[] { "FindFakeProducts" }), "a minion may be handed it");

        // The engine's own check (AgentBuiltTools) reads the names it is given, not core's static list.
        string skills = Directory.CreateTempSubdirectory("catalog-").FullName;
        try
        {
            var options = new AgentOptions
            {
                WorkspaceRoot = skills,
                SkillsDirectory = skills,
                RatesUrl = null
            };
            var engine = new AgentEngine(SkillLoader.Load(options), new FakeChatClient(), options)
            {
                SkillsDirectory = skills,
                ReservedToolNames = catalog.ToolNames
            };
            string refused =
                engine.CreateTool(
                    "# Tool: FindFakeProducts\n- Description: mine\n- Implementation:\n```python\nprint(1)\n```\n");
            Assert.That(refused, Does.Contain("FindFakeProducts").And.Contain("built-in").IgnoreCase);
        }
        finally
        {
            Directory.Delete(skills, recursive: true);
        }
    }

    /// <summary>Public methods of the tool class are the tools; each becomes a function with its description.</summary>
    [Test]
    public void TheOneRule_ForWhichMethodsAreTools_AndHowEachBecomesAFunction()
    {
        var toolset = FakePack.Toolset("fakeshop", typeof(FakeShopTools), _ => [new FakeShopTools()]);

        var methods = AgentCatalog.ToolMethodsOf(toolset).ToList();
        Assert.That(methods.Select(m => m.Name), Is.EqualTo(new[] { "FindFakeProducts" }),
            "public instance methods declared on the class");

        var function = AgentCatalog.FunctionOf(methods[0], _ => new FakeShopTools());
        Assert.That(function.Name, Is.EqualTo("FindFakeProducts"));
        Assert.That(function.Description, Is.EqualTo("Finds fake products."));
        Assert.That(function.JsonSchema.GetRawText(), Does.Contain("query"));
    }

    /// <summary>With no identity and no pack, the catalogue is core's toolsets and nothing else.</summary>
    [Test]
    public void CoreOnly_IsEverythingAsToday()
    {
        var core = AgentCatalog.CoreOnly;

        Assert.That(core.Identities, Is.Empty);
        Assert.That(core.Packs, Is.Empty);
        Assert.That(core.Toolsets, Is.EqualTo(ToolsetCatalog.All));
        Assert.That(core.ToolNames,
            Does.Contain("FetchUrl").And.Contain("ReadWorkspaceFile").And.Not.Contain("CreateTool"));
        Assert.That(core.Resolve([CoreToolset.Files.Wire(), CoreToolset.Git.Wire()], Context()).Select(t => t.Name),
            Is.EqualTo(new[] { CoreToolset.Files.Wire(), CoreToolset.Git.Wire() }));
    }

    /// <summary>The prompt reader takes the same providers again quietly, and refuses new ones after a read.</summary>
    [Test]
    public void Prompts_UsedAgain_DoNothing_AndNewOnesAfterARead_AreRefused()
    {
        // The reader is static (ADR 0018): the host registers every identity's and pack's prompts before the first
        // read; the same providers again are skipped (a host or test that builds twice), new ones after a read fail.
        _ = SkillFileSource.EmbeddedFiles;
        var already = SkillFileSource.Providers;

        Assert.DoesNotThrow(() => SkillFileSource.Use(already));
        Assert.DoesNotThrow(() => SkillFileSource.Use([]));
        var error = Assert.Throws<InvalidOperationException>(() =>
            SkillFileSource.Use([new FakePack("late").Prompts]))!;
        Assert.That(error.Message, Does.Contain("after"));
    }

    /// <summary>A role named in <c>ROLES_ENABLED</c> that this instance doesn't have stops start-up.</summary>
    [Test]
    public void StartUp_RefusesARolesEnabledName_ThisInstanceDoesntHave()
    {
        var options = new AgentOptions
        {
            WorkspaceRoot = TestPaths.RepoRoot,
            SkillsDirectory = Path.Combine(TestPaths.RepoRoot, "Skills"),
            RolesEnabled = ["testprod-specialist", "nope"]
        };

        var error = Assert.Throws<RoleException>(() => RoleFiles.Check(options, RoleCatalog.DefaultVoice))!;

        Assert.That(error.Message, Does.StartWith("ROLES_ENABLED names roles this instance doesn't have: nope."),
            "only the unknown name is named; the known ones follow under Available");
        Assert.DoesNotThrow(() =>
            RoleFiles.Check(
                new AgentOptions
                {
                    WorkspaceRoot = TestPaths.RepoRoot,
                    RolesEnabled = ["testprod-specialist", "testprod/voice-af"]
                },
                RoleCatalog.DefaultVoice));
    }

    /// <summary>A default mode that points at no file stops start-up.</summary>
    [Test]
    public void StartUp_RefusesADefaultMode_ThatResolvesToNoFile()
    {
        var options = new AgentOptions { WorkspaceRoot = TestPaths.RepoRoot };

        Assert.DoesNotThrow(() => RoleFiles.Check(options, RoleCatalog.DefaultVoice),
            "core embeds louis-agent/voice until CL17");
        var error = Assert.Throws<RoleException>(() => RoleFiles.Check(options, "louis-agent/nothing"))!;
        Assert.That(error.Message, Does.Contain("louis-agent/nothing"));
    }
}
