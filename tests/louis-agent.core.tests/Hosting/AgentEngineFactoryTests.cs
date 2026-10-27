using LouisAgent.Testing;
using LouisAgent.Choices;
using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Tests.Tools.Web.Replay;
using LouisAgent.Core.Tools.Web.Replay;

namespace LouisAgent.Core.Tests.Hosting;

// F5 step 5: one engine per toolset profile, cached by its normalised list, with only its tools and its guides.
public class AgentEngineFactoryTests
{
    private static AgentOptions Options(string? paymoKey = null) => new()
    {
        WorkspaceRoot = TestPaths.RepoRoot, SkillsDirectory = Path.Combine(TestPaths.RepoRoot, "Skills"),
        PaymoApiKey = paymoKey, RatesUrl = null,
    };

    private static AgentEngineFactory Factory(int capacity = AgentEngineFactory.DefaultCapacity, string? paymoKey = null) =>
        new(new FakeChatClient(), Options(paymoKey), capacity: capacity);

    private static string[] Names(AgentEngine engine) => engine.Tools.Select(t => t.Name).ToArray();

    /// <summary>A factory with only <paramref name="enabled"/> roles on (F29); every one is built in (CL8 step 4).</summary>
    private static AgentEngineFactory FactoryEnabling(IReadOnlyList<string> enabled)
    {
        var options = Options();
        options.RolesEnabled = enabled;
        return new AgentEngineFactory(new FakeChatClient(), options);
    }

    [Test]
    public void OnlyTheEnabledRoles_AreOffered_AndAnotherIsRefused()
    {
        // F29: Louis switched every specialist but the cook off until it has passed a check like the cook's. The gate is
        // a setting the API enforces, so a prompt slip that names the researcher fails at the peer, not quietly.
        var factory = FactoryEnabling(["testprod/voice-af", "testprod-specialist"]);

        Assert.That(factory.AvailableRoles(RoleLevel.Agent), Is.EqualTo(new[] { "testprod/voice-af" }), "louis-agent/voice is built in but off");
        Assert.That(factory.AvailableRoles(RoleLevel.Specialist), Is.EqualTo(new[] { "testprod-specialist" }), "another role is refused");

        var refused = Assert.Throws<LouisAgent.Core.Skills.Roles.RoleException>(() => factory.GetOrCreate([CoreToolset.Web.Wire()], "testprod-other", RoleLevel.Specialist))!;
        Assert.That(refused.Message, Does.Contain("testprod-other").And.EndWith("Available: testprod-specialist."), "named as unknown, with what's on");
        Assert.That(factory.Profiles, Is.Empty, "refused before anything is built");

        Assert.That(factory.GetOrCreate([CoreToolset.Web.Wire()], "testprod-specialist", RoleLevel.Specialist).SkillDocumentation, Does.Contain("Never invent a fact"));
    }


    [Test]
    public void TheContextHoldsAStoreOnlyWhereARecordingDirectoryIsSet()
    {
        // F18 step 4b.1: one store per process, built from REPLAY_DIRECTORY in one place (ReplayStore.From) and handed to the context.
        var without = new AgentEngineFactory(new FakeChatClient(), Options());
        var with = new AgentEngineFactory(new FakeChatClient(), new AgentOptions { WorkspaceRoot = TestPaths.RepoRoot, RatesUrl = null, ReplayDirectory = "/data/replay" });

        Assert.That(without.Context.Replay, Is.Null);
        Assert.That(with.Context.Replay, Is.Not.Null);
    }

    [Test]
    public void TheSearchProvider_IsRecordedAroundOnlyWhereAStoreIsKept_AHostsOwnIncluded()
    {
        // F18 step 4b.2: a fake given by a host records like a real provider, so a test can prove a replay with one.
        var own = new CountingSearch(name: "Own");
        var options = new AgentOptions { WorkspaceRoot = TestPaths.RepoRoot, RatesUrl = null, ReplayDirectory = "/data/replay" };

        var recorded = new AgentEngineFactory(new FakeChatClient(), options, webSearch: own).Context.WebSearch;
        var plain = new AgentEngineFactory(new FakeChatClient(), Options(), webSearch: own).Context.WebSearch;

        Assert.That(recorded, Is.TypeOf<RecordedSearchProvider>());
        Assert.That(recorded.Name, Is.EqualTo("Own"));
        Assert.That(plain, Is.SameAs(own));
    }

    [Test]
    public async Task TheEnginesGetToday_IsRecordedAndReplayedThroughTheFactorysClock()
    {
        // F18 step 4b.4: one ReplayClock per factory (so per process) reaches the engine's own GetToday. A second factory over the same folder
        // stands in for a later process: it replays the saved time, not its own.
        string folder = Path.Combine(Path.GetTempPath(), "louis-replay-" + Guid.NewGuid().ToString("N"));
        try
        {
            AgentEngineFactory Factory() => new(new FakeChatClient(), new AgentOptions
            {
                WorkspaceRoot = TestPaths.RepoRoot, RatesUrl = null, ReplayDirectory = folder, HostTimeZone = "Africa/Johannesburg",
            });
            async Task<string> TodayAsync() =>
                (await Factory().GetOrCreate([CoreToolset.Files.Wire()]).Tools.OfType<Microsoft.Extensions.AI.AIFunction>().Single(t => t.Name == "GetToday")
                    .InvokeAsync(new Microsoft.Extensions.AI.AIFunctionArguments()))!.ToString()!;

            string recorded, replayed;
            using (SessionReplay.Use(new SessionReplay(WebReplayMode.Record, "t", Model: false))) recorded = await TodayAsync();
            using (SessionReplay.Use(new SessionReplay(WebReplayMode.Replay, "t", Model: false))) replayed = await TodayAsync();

            Assert.That(replayed, Is.EqualTo(recorded));
            Assert.That(File.ReadAllText(Directory.GetFiles(Path.Combine(folder, "trials")).Single()), Does.Contain("\"kind\":\"clock\""));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public async Task TheEnginesModelAnswers_AreRecordedAndReplayedThroughTheFactorysStore()
    {
        // F18 step 4e.3: the factory hands its one store to every engine, so a recording run's answers replay on a later factory (a later
        // process) over the same folder, with a client that throws if the model is asked.
        string folder = Path.Combine(Path.GetTempPath(), "louis-replay-" + Guid.NewGuid().ToString("N"));
        try
        {
            AgentEngineFactory Factory(Microsoft.Extensions.AI.IChatClient client) =>
                new(client, new AgentOptions { WorkspaceRoot = TestPaths.RepoRoot, RatesUrl = null, ReplayDirectory = folder });
            async Task<string> AskAsync(AgentEngineFactory factory)
            {
                var engine = factory.GetOrCreate([]);
                return await engine.ProcessPromptAsync(engine.NewHistory(), "hello");
            }

            string recorded, replayed;
            using (SessionReplay.Use(new SessionReplay(WebReplayMode.Record, "t", Model: false))) recorded = await AskAsync(Factory(new FakeChatClient(_ => Answer("recorded answer"))));
            var throwing = new FakeChatClient(_ => throw new InvalidOperationException("the model was called"));
            using (SessionReplay.Use(new SessionReplay(WebReplayMode.Replay, "t", Model: true))) replayed = await AskAsync(Factory(throwing));

            Assert.That((recorded, replayed), Is.EqualTo(("recorded answer", "recorded answer")));
            Assert.That(throwing.Requests, Is.Empty);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    private static Microsoft.Extensions.AI.ChatResponse Answer(string text) =>
        new(new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, text));

    [Test]
    public async Task ASessionNamingAnLlmProfile_RunsOnItsModel_AndOneWithout_OnTheInstances()
    {
        // F30 (Louis: "can we switch it on session?"): one instance serves Haiku to the cook and a local model to a price
        // lookup, so the model is chosen when a session opens, not per container.
        var own = new FakeChatClient(_ => Answer("haiku"));
        var local = new FakeChatClient(_ => Answer("qwen"));
        var factory = new AgentEngineFactory(own, Options(), sessionLlms: [new SessionLlm("ollama-puls", local)]);

        var onLocal = factory.GetOrCreate([], llm: "ollama-puls");
        Assert.That(await onLocal.ProcessPromptAsync(onLocal.NewHistory(), "prices?"), Is.EqualTo("qwen"));
        var onOwn = factory.GetOrCreate([]);
        Assert.That(await onOwn.ProcessPromptAsync(onOwn.NewHistory(), "hello"), Is.EqualTo("haiku"));

        Assert.That((own.Requests.Count, local.Requests.Count), Is.EqualTo((1, 1)), "each request went to its own model");
        Assert.That(onLocal, Is.Not.SameAs(onOwn), "the profile is part of the engine's key");
        Assert.That(factory.Profiles, Does.Contain("|llm:ollama-puls"));
        Assert.That(factory.SessionLlms, Is.EqualTo(new[] { "ollama-puls" }));
    }

    [Test]
    public void AnUnknownLlmProfile_IsRefused_BeforeAnythingIsBuilt()
    {
        var factory = new AgentEngineFactory(new FakeChatClient(), Options(), sessionLlms: [new SessionLlm("ollama-puls", new FakeChatClient())]);

        var refused = Assert.Throws<SessionLlmException>(() => factory.GetOrCreate([], llm: "gpt-local"))!;

        Assert.That(refused.Message, Does.Contain("gpt-local").And.Contain("ollama-puls"), "named, with what this instance has");
        Assert.That(factory.Profiles, Is.Empty);
    }

    [Test]
    public void TheSessionProfiles_AreReadFromTheEnvironment_TrimmedAndLowercase()
    {
        var options = AgentOptions.FromEnvironment(name => name == "LLM_SESSION_PROFILES" ? " Ollama-Puls , ollama-puls," : null);

        Assert.That(options.SessionLlmProfiles, Is.EqualTo(new[] { "ollama-puls" }));
        Assert.That(AgentOptions.FromEnvironment(_ => null).SessionLlmProfiles, Is.Empty);
    }

    [Test]
    public void AnEmptyEnabledList_MeansEveryRole()
    {
        var factory = FactoryEnabling([]);

        Assert.That(factory.AvailableRoles(RoleLevel.Agent), Is.EqualTo(new[] { "louis-agent/voice", "louis-agent/voice-orchestrator", "testprod/voice-af", "testprod/voice-en" }));
        Assert.That(factory.DefaultVoice, Is.EqualTo("louis-agent/voice"), "GET /v1/roles' default");
        Assert.That(factory.AvailableRoles(RoleLevel.Specialist), Does.Contain("testprod-specialist"));
    }

    [Test]
    public void TheEnabledList_IsReadFromTheEnvironment_TrimmedAndLowercase()
    {
        var options = AgentOptions.FromEnvironment(name => name == "PERSONALITIES_ENABLED" ? " " + SampleRole.Beta + ", " + SampleRole.Alpha.ToUpperInvariant() + " ," + SampleRole.Beta + ", " : null);

        Assert.That(options.RolesEnabled, Is.EqualTo(new[] { SampleRole.Beta, SampleRole.Alpha }));
        Assert.That(AgentOptions.FromEnvironment(_ => null).RolesEnabled, Is.Empty);
    }

    [Test]
    public void TheSameList_InAnyOrderOrCase_GivesTheSameEngine()
    {
        var factory = Factory();

        var first = factory.GetOrCreate([CoreToolset.Git.Wire(), CoreToolset.Files.Wire()]);
        var second = factory.GetOrCreate(["Files", " git ", CoreToolset.Files.Wire()]);

        Assert.That(second, Is.SameAs(first));
        Assert.That(factory.Profiles, Is.EqualTo(new[] { "files,git" }));
    }

    [Test]
    public void ARole_IsItsOwnProfile_WithItsOwnPrompt()
    {
        // F21: the same toolsets with another role, or at another level (CL8: no router below level 0), is another engine.
        var factory = Factory();

        var plain = factory.GetOrCreate([CoreToolset.Files.Wire()]);
        var voiced = factory.GetOrCreate([CoreToolset.Files.Wire()], "testprod/voice-af");
        var noRouter = factory.GetOrCreate([CoreToolset.Files.Wire()], level: RoleLevel.Specialist);

        Assert.That(voiced, Is.Not.SameAs(plain));
        Assert.That(factory.GetOrCreate([CoreToolset.Files.Wire()], "testprod/voice-af"), Is.SameAs(voiced));
        Assert.That(voiced.SkillDocumentation, Does.Contain("# Testprod - Stem").And.Not.Contain("Louis's Minion"));
        Assert.That(noRouter.SkillDocumentation, Does.Not.Contain("# Louis Agent - Default Instructions"));
        Assert.That(plain.SkillDocumentation, Does.Contain("# Louis Agent - Default Instructions"));
        Assert.That(factory.Profiles, Is.EquivalentTo(new[] { CoreToolset.Files.Wire(), "files|testprod/voice-af", "files|specialist" }));
    }

    [Test]
    public void AnUnknownRole_IsRefused_BeforeAnythingIsBuilt()
    {
        var factory = Factory();

        Assert.Throws<LouisAgent.Core.Skills.Roles.RoleException>(() => factory.GetOrCreate([CoreToolset.Files.Wire()], "nobody-here"));
        Assert.That(factory.Profiles, Is.Empty);
    }

    [Test]
    public void DifferentLists_GiveDifferentTools()
    {
        var factory = Factory();

        var files = factory.GetOrCreate([CoreToolset.Files.Wire()]);
        var git = factory.GetOrCreate([CoreToolset.Git.Wire()]);

        Assert.That(Names(files), Does.Contain("ReadWorkspaceFile").And.Not.Contain("GetStatus"));
        Assert.That(Names(git), Does.Contain("GetStatus").And.Not.Contain("ReadWorkspaceFile"));
        Assert.That(Names(files), Does.Contain("ExecuteSkill").And.Not.Contain("CreateTool"), "no skills toolset, no self-extension");
    }

    [Test]
    public void TheSkillsToolset_AddsTheSelfExtensionTools()
    {
        Assert.That(Names(Factory().GetOrCreate([CoreToolset.Files.Wire(), CoreToolset.Skills.Wire()])), Does.Contain("CreateTool").And.Contain("CreateSkillFile"));
    }

    [Test]
    public void AnEngine_HasOnlyItsToolsetsGuides_AndReloadingKeepsThem()
    {
        var engine = Factory().GetOrCreate([CoreToolset.Files.Wire(), CoreToolset.Skills.Wire()]);

        Assert.That(engine.SkillDocumentation, Does.Contain("# Files Skills").And.Not.Contain("# Python Skills"));
        engine.ReloadSkills();
        Assert.That(engine.SkillDocumentation, Does.Contain("# Files Skills").And.Not.Contain("# Python Skills"));
    }

    [Test]
    public void AnUnknownOrUnavailableToolset_ThrowsBeforeAnythingIsBuilt()
    {
        var factory = Factory();

        Assert.That(() => factory.GetOrCreate([CoreToolset.Files.Wire(), "nope"]), Throws.TypeOf<ToolsetException>().With.Message.Contain("Valid names"));
        Assert.That(() => factory.GetOrCreate([CoreToolset.Paymo.Wire()]), Throws.TypeOf<ToolsetException>().With.Message.Contain("PAYMO_API_KEY"));
        Assert.That(factory.Profiles, Is.Empty);
    }

    [Test]
    public void AFullCache_DropsTheLeastRecentlyUsed_NotTheNewList()
    {
        // PR #27 review: on a specialist the model chooses the lists; refusing the 17th would stop it until a restart.
        var factory = Factory(capacity: 2);
        var files = factory.GetOrCreate([CoreToolset.Files.Wire()]);
        factory.GetOrCreate([CoreToolset.Git.Wire()]);
        factory.GetOrCreate([CoreToolset.Files.Wire()]);   // files is now the more recent

        var dotnet = factory.GetOrCreate([CoreToolset.DotNet.Wire()]);

        Assert.That(dotnet, Is.Not.Null);
        Assert.That(factory.Profiles, Is.EquivalentTo(new[] { CoreToolset.Files.Wire(), CoreToolset.DotNet.Wire() }), "git, the least recently used, went");
        Assert.That(factory.GetOrCreate([CoreToolset.Files.Wire()]), Is.SameAs(files), "a kept profile is the same engine");
        Assert.That(factory.GetOrCreate([CoreToolset.Git.Wire()]), Is.Not.Null, "a dropped one is built again");
    }

    [Test]
    public void Available_ListsOnlyToolsetsWhoseSettingsAreSet()
    {
        Assert.That(Factory().Available, Does.Contain(CoreToolset.Files.Wire()).And.Not.Contain(CoreToolset.Paymo.Wire()));
        Assert.That(Factory(paymoKey: "key").Available, Does.Contain(CoreToolset.Paymo.Wire()));
    }

    [Test]
    public void NoToolsets_GivesOnlyExecuteSkill()
    {
        Assert.That(Names(Factory().GetOrCreate([])), Is.EquivalentTo(new[] { "ExecuteSkill", "GetToday" }));
    }

    [Test]
    public void AMinionsEngine_HasOneTool_TheRoleOnTheBase_NoGuides_AndItsOwnKey()
    {
        // F30 step 4 (Louis: a specialist hands a minion one task and one of its own tools): the engine is the role on
        // the minion base with that one tool, no toolset guide and no router, so a 4B model reads a prompt it can follow.
        var factory = Factory();

        var minion = factory.GetOrCreate([CoreToolset.Web.Wire()], "testprod-minion-lookup", RoleLevel.Minion, tool: "FetchUrl");

        Assert.That(Names(minion), Is.EqualTo(new[] { "FetchUrl" }), "one tool: no clock, no ExecuteSkill, no WebSearch");
        Assert.That(minion.SkillDocumentation, Does.Contain("# Testprod minion: Lookup").And.Contain("You are a **minion**")
            .And.Not.Contain("# Web Skills").And.Not.Contain("# Louis Agent - Default Instructions"));
        Assert.That(factory.Profiles, Is.EqualTo(new[] { "web|testprod-minion-lookup|minion|tool:FetchUrl" }));
        Assert.That(factory.GetOrCreate([CoreToolset.Web.Wire()], "testprod-minion-lookup", RoleLevel.Minion, tool: "FetchUrl"), Is.SameAs(minion));
        Assert.That(factory.GetOrCreate([CoreToolset.Web.Wire()], "testprod-minion-lookup", RoleLevel.Minion, tool: "ReadRenderedPage"), Is.Not.SameAs(minion),
            "the tool is part of the key");
    }

    [Test]
    public void AToolTheToolsetsDontHave_IsRefused_BeforeAnythingIsBuilt()
    {
        var factory = Factory();

        var ex = Assert.Throws<ToolsetException>(() =>
            factory.GetOrCreate([CoreToolset.Web.Wire()], "testprod-minion-lookup", RoleLevel.Minion, tool: "ReadWorkspaceFile"))!;

        Assert.That(ex.Message, Does.Contain("ReadWorkspaceFile").And.Contain(CoreToolset.Web.Wire()));
        Assert.That(factory.Profiles, Is.Empty);
    }
}
