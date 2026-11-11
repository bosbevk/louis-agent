using LouisAgent.Core.Web;
using LouisAgent.Packs.Web;
using LouisAgent.Identities.Puls;
using LouisAgent.Packs.Calculator;
using LouisAgent.Packs.Recipes;
using LouisAgent.Choices;
using LouisAgent.Api.Contracts.Info;
using LouisAgent.SessionStream.Wire.Replies;
using LouisAgent.Api.Endpoints.Info;
using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Delegation;
using LouisAgent.Packs.ShopPrices;
using LouisAgent.Packs.WhereToBuy;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// A session picks its toolsets when it opens and keeps them, and sessions with different lists don't share tools.
/// No model, network or web host: the sessions run on a factory whose client is never called.
/// </summary>
public class AgentSessionsTests
{
    /// <summary>
    /// The repo's root folder, so the sessions find the real skills.
    /// </summary>
    private static string RepoRoot { get; } =
        TestPaths.RepoRoot;

    /// <summary>
    /// What the Puls stack runs with: Puls and the packs it names (where to buy and shop prices, F50.2, and the
    /// recipes, with every site on: RECIPE_SOURCES=*, as the compose sets it, so the cook's required `recipes`
    /// resolves once a browser is given, BrowserUrl in Sessions), and a pack whose toolset is known but off on this
    /// instance, as a pack that misses its setting is.
    /// </summary>
    private static AgentCatalog PulsCatalog() =>
        new([new PulsIdentity()],
        [
            new WebPack(), TestTools.Pack,
            new WhereToBuyPack(), new ShopPricesPack(new ShopPricesOptions()), new CalculatorPack(),
            new RecipesPack(new RecipesOptions { RecipeSources = "*" }),
            new FakePack(OffPack,
            [
                FakePack.Toolset(OffPack, typeof(FakeShopTools), _ => [new FakeShopTools()]) with
                {
                    MissingSettings = _ => ["OFF_PACK_URL"]
                }
            ])
        ]);

    /// <summary>The pack this instance knows but hasn't set up.</summary>
    private const string OffPack = "offpack";

    /// <summary>
    /// Fresh sessions on the repo's skills and the Puls catalogue, with a model that's never called.
    /// </summary>
    private static AgentSessions Sessions(IReadOnlyList<SessionLlm>? llms = null) =>
        new(new AgentEngineFactory(new NoModel(), PulsOptions(), services: new HostServices(WebAccess.Create),
            sessionLlms: llms, catalog: PulsCatalog()));

    /// <summary>
    /// The settings a Puls stack gives the cook: the browser the recipes toolset reads through, a peer for its minions
    /// (this instance itself) and the find-recipe minion on the instance's own model (F35.2 step 2b: the cook's header
    /// asks for `minions`, which needs both).
    /// </summary>
    private static AgentOptions PulsOptions() =>
        new()
        {
            WorkspaceRoot = RepoRoot,
            SkillsDirectory = Path.Combine(RepoRoot, "demos/puls/skills"),
            RatesUrl = null,
            BrowserUrl = "http://puls-browser:9222",
            AgentPeers = new Dictionary<string, Uri> { ["puls"] = new("http://puls:8080/") },
            MinionLlms = new Dictionary<string, IReadOnlyList<string>>
            {
                [PulsRole.MinionFindRecipe.Wire()] = [MinionTools.InstanceLlm]
            },
        };

    /// <summary>
    /// The names of the tools a session's model can call.
    /// </summary>
    private static string[] Names(AgentSession session) => session.Engine.Tools.Select(t => t.Name).ToArray();

    /// <summary>
    /// A session that asks for files gets the file tools and none of git's.
    /// </summary>
    [Test]
    public void ASessionWithFiles_HasNoGitTools()
    {
        var session = Sessions().Create([TestToolset.Files.Wire()]);

        Assert.That(session.Toolsets, Is.EqualTo(new[] { TestToolset.Files.Wire() }));
        Assert.That(Names(session), Does.Contain("ReadWorkspaceFile").And.Not.Contain("GetStatus"));
    }

    /// <summary>
    /// Naming no toolsets gives you the default, which is just the skills.
    /// </summary>
    [Test]
    public void NoToolsets_GivesTheDefault_SkillsOnly()
    {
        var session = Sessions().Create();

        Assert.That(session.Toolsets, Is.EqualTo(AgentSessions.DefaultToolsets));
        Assert.That(Names(session), Does.Contain("CreateTool").And.Not.Contain("ReadWorkspaceFile"));
    }

    /// <summary>
    /// An empty list really means none: only the two tools every session has.
    /// </summary>
    [Test]
    public void AnEmptyList_GivesNoToolsets()
    {
        Assert.That(Names(Sessions().Create([])), Is.EquivalentTo(new[] { "ExecuteSkill", "GetToday" }));
    }

    /// <summary>
    /// Sessions with different toolsets get different engines; two with the same list share one engine but each keeps
    /// its own history.
    /// </summary>
    [Test]
    public void TwoSessionsWithDifferentLists_DontShareTools_TwoWithTheSameShareAnEngine()
    {
        var sessions = Sessions();

        var files = sessions.Create([TestToolset.Files.Wire()]);
        var git = sessions.Create([TestToolset.Git.Wire()]);
        var filesAgain = sessions.Create(["FILES"]);

        Assert.That(git.Engine, Is.Not.SameAs(files.Engine));
        Assert.That(Names(git), Does.Not.Contain("ReadWorkspaceFile"));
        Assert.That(filesAgain.Engine, Is.SameAs(files.Engine), "one engine, one cacheable prefix per profile");
        Assert.That(filesAgain.History, Is.Not.SameAs(files.History), "but each session its own history");
    }

    /// <summary>
    /// A toolset we don't know is refused, and the refusal lists the ones we do.
    /// </summary>
    [Test]
    public void AnUnknownToolset_IsRefused_WithTheValidNames()
    {
        Assert.That(() => Sessions().Create([TestToolset.Files.Wire(), "nope"]),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("Valid names").And.Message
                .Contain(TestToolset.Files.Wire()));
    }

    /// <summary>
    /// An optional toolset this instance doesn't have is quietly left out, and the session lists only what it loaded.
    /// </summary>
    [Test]
    public void AnOptionalToolset_ThisInstanceLacks_IsLeftOut_AndTheSessionSaysWhatItLoaded()
    {
        // F33 step 4: a parent may hand an optional toolset; here its pack misses its setting, so the session gets
        // the web alone.
        var session = Sessions().Create([TestToolset.Web.Wire(), OffPack + "?"], depth: 1);

        Assert.That(session.Toolsets, Is.EqualTo(new[] { TestToolset.Web.Wire() }));
        Assert.That(Names(session), Does.Contain("WebSearch").And.Not.Contain("FindProducts"));
    }

    /// <summary>
    /// The cook brings its own toolsets: Puls hands it the web, and its role adds the recipes, where to buy and the
    /// shop prices.
    /// </summary>
    [Test]
    public void TheCook_LoadsWhereToBuyAndShopPrices_FromItsRole()
    {
        // F35.2 steps 1 and 2b and F50.2: the cook's header names its recipes, both packs' toolsets and the minions,
        // so they come with the role, beside what Puls passed.
        var cook = Sessions().Create([TestToolset.Web.Wire()], depth: 1, role: PulsRole.Cook.Wire());

        Assert.That(cook.Toolsets,
            Is.EquivalentTo(new[]
            {
                TestToolset.Web.Wire(), RecipesToolset.Recipes.Wire(), WhereToBuyToolset.WhereToBuy.Wire(),
                ShopPricesToolset.ShopPrices.Wire(), CalculatorToolset.Calculator.Wire()
            }));
        Assert.That(Names(cook),
            Does.Contain("FindBranches").And.Contain("FindShopsNear").And.Contain("EstimatePrices").And
                .Contain("WebSearch").And.Contain("Calculate").And.Not.Contain("StartMinion"));
    }

    /// <summary>
    /// Marking specialists as optional doesn't sneak delegation into a sub-session.
    /// </summary>
    [Test]
    public void AnOptionalMark_DoesntGetPastTheDelegationRules() =>
        Assert.That(() => Sessions().Create(["specialists?"], depth: 1),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("delegates"));

    /// <summary>
    /// A sub-session can't delegate further, the tree stops at the minion, and the old "agents" name is refused.
    /// </summary>
    [Test]
    public void ASubSession_CantLoadSpecialists_AndDepthStopsAtTheMinion()
    {
        var sessions = Sessions();

        Assert.That(() => sessions.Create([CoreToolset.Specialists.Wire()], depth: 1),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("delegates"));
        Assert.That(() => sessions.Create([TestToolset.Files.Wire()], depth: 3),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("between 0 and 2"));
        Assert.That(sessions.Create([TestToolset.Files.Wire()], depth: 1).Depth, Is.EqualTo(1));
        // CL9 step 5: a client still sending the old name gets the 400 that lists the valid toolsets.
        Assert.That(() => sessions.Create(["agents"]),
            Throws.TypeOf<ToolsetException>().With.Message.Contain(CoreToolset.Specialists.Wire()));
    }

    /// <summary>
    /// You can look a session up by its id until it's removed.
    /// </summary>
    [Test]
    public void ASession_IsFoundById_UntilRemoved()
    {
        var sessions = Sessions();
        var session = sessions.Create([TestToolset.Files.Wire()]);

        Assert.That(sessions.Get(session.Id), Is.SameAs(session));
        Assert.That(sessions.Remove(session.Id), Is.True);
        Assert.That(sessions.Get(session.Id), Is.Null);
    }

    /// <summary>
    /// Only a top-level session gets the routing instructions; a sub-session keeps just its toolsets' guides.
    /// </summary>
    [Test]
    public void ASubSession_HasNoRouter_ATopLevelOneHas()
    {
        // F21: default.md picks toolsets and delegates; a sub-session's toolsets were chosen for it and it can't
        // delegate.
        var sessions = Sessions();

        var top = sessions.Create([TestToolset.Files.Wire()]);
        var sub = sessions.Create([TestToolset.Files.Wire()], depth: 1);

        Assert.That(top.Engine.SkillDocumentation, Does.Contain("# Puls - Routing"));
        Assert.That(sub.Engine.SkillDocumentation, Does.Not.Contain("# Puls - Routing"));
        Assert.That(sub.Engine.SkillDocumentation, Does.Contain("# Files Skills"), "its toolsets' guides stay");
    }

    /// <summary>
    /// A role we don't know is refused.
    /// </summary>
    [Test]
    public void AnUnknownRole_IsRefused()
    {
        Assert.Throws<RoleException>(() =>
            Sessions().Create([TestToolset.Files.Wire()], role: "nobody-here"));
    }

    /// <summary>
    /// A role opens only at its own level: the cook as a specialist, Puls's voice at the top.
    /// </summary>
    [Test]
    public void ARole_OpensOnlyAtItsOwnLevel()
    {
        // CL8: a session's level is its depth; the cook is a specialist (level 1), Puls's voice is level 0's.
        var sessions = Sessions();

        Assert.Throws<RoleException>(() =>
            sessions.Create([TestToolset.Web.Wire()], role: PulsRole.Cook.Wire()));
        Assert.Throws<RoleException>(() =>
            sessions.Create([TestToolset.Web.Wire()], depth: 1, role: "puls/voice-af"));
        Assert.That(sessions.Create([TestToolset.Web.Wire()], role: "puls/voice-af").Role, Is.EqualTo("puls/voice-af"));
        Assert.That(sessions.Create([TestToolset.Web.Wire()], depth: 1, role: PulsRole.Cook.Wire()).Role,
            Is.EqualTo(PulsRole.Cook.Wire()));
    }

    /// <summary>
    /// A session that names no role gets the default voice.
    /// </summary>
    [Test]
    public void NoRole_IsTheDefault()
    {
        var session = Sessions().Create([TestToolset.Files.Wire()], role: "");

        Assert.That(session.Role, Is.Null);
        Assert.That(session.Engine.SkillDocumentation, Does.Contain("# Puls - Stem"));
    }

    /// <summary>
    /// A session remembers its attachments by name, whatever the case, keeps the newest, and cuts a huge one short.
    /// </summary>
    [Test]
    public void ASession_RemembersItsAttachments_ByName_Capped()
    {
        // F22: a later turn can still hand an earlier attachment to a specialist.
        var session = Sessions().Create([TestToolset.Files.Wire()]);

        session.Remember("Verslag.txt", "eerste");
        session.Remember("verslag.txt", "tweede");
        session.Remember("groot.txt", new string('x', AgentSession.MaxAttachmentChars + 10));

        Assert.That(session.Attachments["VERSLAG.TXT"], Is.EqualTo("tweede"), "names ignore case; the newest wins");
        Assert.That(session.Attachments["groot.txt"], Has.Length.EqualTo(AgentSession.MaxAttachmentChars));
    }

    /// <summary>
    /// The user's place carries their location only when they shared it, and their time zone whenever it's sent.
    /// </summary>
    [Test]
    public void ThePlace_HasTheLocationWhenShared_AndTheTimeZoneAlways()
    {
        // F22 step 2b, and PR #28's review: a time zone sent without a location is kept (GetToday in the user's time).
        Assert.That(
            new CreateSessionRequest(null, null, null, Location: " Pretoria ", TimeZone: "Africa/Johannesburg").Place(),
            Is.EqualTo(new LouisAgent.Core.Sessions.UserPlace("Pretoria", "Africa/Johannesburg")));
        Assert.That(new CreateSessionRequest(null, null, null, TimeZone: "Europe/London").Place(),
            Is.EqualTo(new LouisAgent.Core.Sessions.UserPlace(null, "Europe/London")));
        Assert.That(new CreateSessionRequest(null, null, null).Place(), Is.Null);

        var session = Sessions().Create([TestToolset.Files.Wire()],
            place: new LouisAgent.Core.Sessions.UserPlace("Pretoria", null));
        Assert.That(session.Place!.Location, Is.EqualTo("Pretoria"));
    }

    /// <summary>
    /// A session keeps the replay it was opened with for all its turns, and one opened without has none.
    /// </summary>
    [Test]
    public void ASessionKeepsTheReplayItWasOpenedWith()
    {
        // F18 step 4a: resolved once when the session opens (ReplayResolution), then kept for its turns.
        var replay =
            new SessionReplay(WebReplayMode.Record,
                "run_1:cook:1/bobotie", false);

        Assert.That(Sessions().Create([TestToolset.Files.Wire()], replay: replay).Replay, Is.EqualTo(replay));
        Assert.That(Sessions().Create([TestToolset.Files.Wire()]).Replay, Is.Null);
    }

    /// <summary>
    /// Writes a view as the JSON the API would send, so a test can check the words callers see.
    /// </summary>
    private static string Json(object view) => System.Text.Json.JsonSerializer.Serialize(view, SessionJson.SnakeCase);

    /// <summary>
    /// What the API sends for a session says "role", never the old "personality".
    /// </summary>
    [Test]
    public void TheSessionJson_SaysRole_NotPersonality()
    {
        var cook = Sessions().Create([TestToolset.Web.Wire()], depth: 1, role: PulsRole.Cook.Wire());

        Assert.That(Json(SessionCreatedReply.Of(cook)),
            Does.Contain("\"role\":\"puls-cook\"").And.Not.Contain("personality"));
        Assert.That(Json(SessionReply.Of(cook)),
            Does.Contain("\"role\":\"puls-cook\"").And.Not.Contain("personality"));
    }

    /// <summary>
    /// A specialist the chat keeps open shows up in the session's JSON with its role.
    /// </summary>
    [Test]
    public async Task ASubSessionTheChatKeeps_IsListedWithItsRole()
    {
        var session = Sessions().Create([TestToolset.Files.Wire()]);
        await session.SubSessions.AddAsync(Kept("sess_cook", []));

        Assert.That(Json(SessionReply.Of(session)),
            Does.Contain("\"role\":\"puls-cook\"").And.Not.Contain("personality"));
    }

    /// <summary>
    /// The roles list names the default and groups the roles by level.
    /// </summary>
    [Test]
    public void TheRolesBody_HasTheDefaultAndTheRolesPerLevel()
    {
        // The roles body, served at GET /v1/roles (CL14's word; the /personalities alias went with CL20, ADR 0039).
        var engines = new AgentEngineFactory(new NoModel(),
            new AgentOptions
            {
                WorkspaceRoot = RepoRoot,
                SkillsDirectory = Path.Combine(RepoRoot, "demos/puls/skills"),
                RatesUrl = null
            }, services: new HostServices(WebAccess.Create), catalog: PulsCatalog());

        string roles = System.Text.Json.JsonSerializer.Serialize(RolesReply.Of(engines), InfoJson.SnakeCase);

        Assert.That(roles,
            Does.Contain("\"default\":\"puls/voice-af\"").And.Contain("\"specialists\":[").And
                .Contain(PulsRole.Cook.Wire()));
    }

    /// <summary>
    /// A cook sub-session the chat keeps open; closing it writes its id into the list you pass, so a test can see it
    /// was closed.
    /// </summary>
    private static LouisAgent.Core.Sessions.SubSessions.OpenSubSession Kept(string sessionId, List<string> closed) =>
        new(new Uri("http://puls-agent/"), sessionId, "puls-agent", [TestToolset.Web.Wire()], PulsRole.Cook.Wire(),
            () =>
            {
                closed.Add(sessionId);
                return Task.CompletedTask;
            });

    /// <summary>
    /// Deleting a chat closes the specialists it kept open on the peer too.
    /// </summary>
    [Test]
    public async Task RemovingASession_ClosesTheSubSessionsItKept()
    {
        // F26 step 3 (F26-S3): the chat is deleted, so its specialists are deleted on the peer too.
        var sessions = Sessions();
        var session =
            sessions.Create([TestToolset.Files.Wire()]); // the test factory has no peers; the registry doesn't care
        var closed = new List<string>();
        await session.SubSessions.AddAsync(Kept("sess_cook", closed));

        Assert.That(sessions.Remove(session.Id), Is.True);

        Assert.That(closed, Is.EqualTo(new[] { "sess_cook" }));
        Assert.That(session.SubSessions.Open, Is.Empty);
    }

    /// <summary>
    /// A session left idle too long is dropped, and its specialists are closed with it.
    /// </summary>
    [Test]
    public async Task AnIdleSession_IsDropped_WithItsSubSessions()
    {
        var sessions = Sessions();
        var session = sessions.Create([TestToolset.Files.Wire()]);
        var closed = new List<string>();
        await session.SubSessions.AddAsync(Kept("sess_cook", closed));

        int dropped = sessions.DropIdle(DateTimeOffset.UtcNow + AgentSessions.IdleTimeout + TimeSpan.FromMinutes(1));

        Assert.That(dropped, Is.EqualTo(1));
        Assert.That(sessions.Get(session.Id), Is.Null);
        Assert.That(closed, Is.EqualTo(new[] { "sess_cook" }));
    }

    /// <summary>
    /// A sub-session gets a role only when its parent names one; without, it has just the default voice.
    /// </summary>
    [Test]
    public void ASubSessionWithoutARole_GetsNone_AndAGivenOneIsKept()
    {
        // CL10: a role is named by the parent, never derived from the toolsets; with none, the default mode's voice
        // only.
        var sessions = Sessions();

        var unnamed = sessions.Create([TestToolset.Web.Wire()], depth: 1);
        Assert.That(unnamed.Role, Is.Null);
        Assert.That(unnamed.Engine.SkillDocumentation,
            Does.Contain("# Puls - Stem").And.Not.Contain("# Puls - Routing"));
        Assert.That(sessions.Create([TestToolset.Web.Wire()], depth: 1, role: PulsRole.Cook.Wire()).Role,
            Is.EqualTo(PulsRole.Cook.Wire()));
        Assert.That(sessions.Create([TestToolset.Web.Wire()]).Role, Is.Null,
            "a top-level session that names none gets the default voice");
    }

    /// <summary>
    /// A session can run on another model by naming its profile, and gets its own engine for it.
    /// </summary>
    [Test]
    public void ASessionNamingAnLlmProfile_KeepsIt_AndOneWithout_HasNone()
    {
        // F30: a price lookup runs on the local model, the chat that asked for it on the instance's.
        var sessions = Sessions([new SessionLlm("ollama-puls", new NoModel())]);

        var local = sessions.Create([TestToolset.Web.Wire()], depth: 1, llm: "ollama-puls");
        var own = sessions.Create([TestToolset.Web.Wire()], depth: 1);

        Assert.That(local.Llm, Is.EqualTo("ollama-puls"));
        Assert.That(own.Llm, Is.Null);
        Assert.That(local.Engine, Is.Not.SameAs(own.Engine), "one engine per model");
    }

    /// <summary>
    /// A model profile we don't have is refused, and the refusal names the ones we do.
    /// </summary>
    [Test]
    public void AnUnknownLlmProfile_IsRefused_NamingTheOnesThereAre()
    {
        var sessions = Sessions([new SessionLlm("ollama-puls", new NoModel())]);

        var ex = Assert.Throws<SessionLlmException>(() => sessions.Create([TestToolset.Web.Wire()], llm: "gpt-9"));
        Assert.That(ex!.Message, Does.Contain("gpt-9").And.Contain("ollama-puls"));
    }

    /// <summary>
    /// The usage tags a sub-session carries to say which session sent it.
    /// </summary>
    private static UsageTags ParentOf(AgentSession parent) => new(null, null, null, $"{parent.Id}#1");

    /// <summary>
    /// A minion opens under its specialist with just one of that specialist's tools and its own guide.
    /// </summary>
    [Test]
    public void AMinion_OpensAtDepthTwo_WithOneToolItsParentHas()
    {
        // F30 step 4: a specialist hands a minion one task and one of its own tools; the peer checks the tool against
        // the parent session it finds here (minions run on the specialist's own instance).
        var sessions = Sessions();
        var cook = sessions.Create([TestToolset.Web.Wire()], depth: 1, role: PulsRole.Cook.Wire());

        var minion = sessions.Create([TestToolset.Web.Wire()], ParentOf(cook), depth: 2,
            role: PulsRole.MinionWebPriceLookup.Wire(), tool: "FetchUrl");

        Assert.That(minion.Depth, Is.EqualTo(2));
        Assert.That(minion.Tool, Is.EqualTo("FetchUrl"));
        Assert.That(Names(minion), Is.EqualTo(new[] { "FetchUrl" }));
        Assert.That(minion.Engine.SkillDocumentation,
            Does.Contain("# Puls minion: Web price lookup").And.Not.Contain("# Web Skills"));
        Assert.That(cook.Tool, Is.Null, "a specialist has toolsets, not one tool");
    }

    /// <summary>
    /// A minion can never do more than the specialist that sent it: only the parent's own tools, and never one that
    /// delegates or extends the agent.
    /// </summary>
    [Test]
    public void AMinion_GetsOnlyAToolItsParentHas_AndOneThatIsEverHandedDown()
    {
        // Capability delegation: a minion can never do more than the specialist that sent it. The parent's toolbox is
        // the only list (Louis: "the specialist must give him its tools"); delegation and self-extension never go down.
        var sessions = Sessions();
        var withFiles = sessions.Create([TestToolset.Files.Wire()], depth: 1, role: PulsRole.Cook.Wire());
        var withWeb = sessions.Create([TestToolset.Web.Wire(), TestToolset.Files.Wire()], depth: 1,
            role: PulsRole.Cook.Wire());

        Assert.That(
            () => sessions.Create([TestToolset.Web.Wire()], ParentOf(withFiles), depth: 2,
                role: PulsRole.MinionWebPriceLookup.Wire(), tool: "FetchUrl"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("parent session has no 'FetchUrl'"));
        Assert.That(
            sessions.Create([TestToolset.Files.Wire()], ParentOf(withWeb), depth: 2,
                role: PulsRole.MinionWebPriceLookup.Wire(), tool: "ReadWorkspaceFile").Tool,
            Is.EqualTo("ReadWorkspaceFile"), "any tool of the parent's own: which one is the specialist's call");
        Assert.That(
            () => sessions.Create([CoreToolset.Skills.Wire()], ParentOf(withWeb), depth: 2,
                role: PulsRole.MinionWebPriceLookup.Wire(), tool: "ExecuteSkill"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("never handed"));
    }

    /// <summary>
    /// A minion needs a parent we can find, a minion's role and its one tool, and it can't go any deeper.
    /// </summary>
    [Test]
    public void AMinion_NeedsAParentItCanFind_AMinionRole_AndATool_AndNothingDeeper()
    {
        var sessions = Sessions();
        var cook = sessions.Create([TestToolset.Web.Wire()], depth: 1, role: PulsRole.Cook.Wire());
        var parent = ParentOf(cook);

        Assert.That(
            () => sessions.Create([TestToolset.Web.Wire()], depth: 2, role: PulsRole.MinionWebPriceLookup.Wire(),
                tool: "FetchUrl"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("parent"), "no parent");
        Assert.That(
            () => sessions.Create([TestToolset.Web.Wire()], new UsageTags(null, null, null, "sess_gone#1"), depth: 2,
                role: PulsRole.MinionWebPriceLookup.Wire(), tool: "FetchUrl"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("sess_gone"), "a parent it can't find");
        Assert.That(
            () => sessions.Create([TestToolset.Web.Wire()], parent, depth: 2, role: PulsRole.Cook.Wire(),
                tool: "FetchUrl"),
            Throws.TypeOf<RoleException>(), "a specialist's role is no minion role");
        Assert.That(
            () => sessions.Create([TestToolset.Web.Wire()], parent, depth: 2, role: "puls-minion", tool: "FetchUrl"),
            Throws.TypeOf<RoleException>(), "the base is no role");
        Assert.That(() => sessions.Create([TestToolset.Web.Wire()], parent, depth: 2, tool: "FetchUrl"),
            Throws.TypeOf<RoleException>(), "a minion always has a role");
        Assert.That(
            () => sessions.Create([TestToolset.Web.Wire()], parent, depth: 2,
                role: PulsRole.MinionWebPriceLookup.Wire()),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("'tool'"), "a minion always has its one tool");
        Assert.That(
            () => sessions.Create([TestToolset.Web.Wire()], depth: 1, role: PulsRole.Cook.Wire(), tool: "FetchUrl"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("'tool'"),
            "a specialist loads toolsets, not a tool");
        Assert.That(
            () => sessions.Create([TestToolset.Web.Wire()], parent, depth: 3,
                role: PulsRole.MinionWebPriceLookup.Wire(), tool: "FetchUrl"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("between 0 and 2"), "a minion never delegates");
    }

    /// <summary>
    /// A minion works out its toolset from the tool it's handed, and only a specialist can send minions.
    /// </summary>
    [Test]
    public void AMinionSession_DerivesItsToolsetFromItsTool_AndMinionsAreForSpecialistsOnly()
    {
        // F30 step 5: StartMinion sends no toolsets, only the tool; the peer finds the toolset that builds it. The
        // minions toolset is a specialist's: never the chat's (level 0), never a minion's (the end of the tree).
        var sessions = Sessions();
        var cook = sessions.Create([TestToolset.Web.Wire()], depth: 1, role: PulsRole.Cook.Wire());

        var minion = sessions.Create(null, ParentOf(cook), depth: 2, role: PulsRole.MinionWebPriceLookup.Wire(),
            tool: "FetchUrl");
        Assert.That(minion.Toolsets, Is.EqualTo(new[] { TestToolset.Web.Wire() }));
        Assert.That(Names(minion), Is.EqualTo(new[] { "FetchUrl" }));

        Assert.That(() => sessions.Create([CoreToolset.Minions.Wire()]),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("specialist"));
        Assert.That(
            () => sessions.Create([CoreToolset.Minions.Wire()], ParentOf(cook), depth: 2,
                role: PulsRole.MinionWebPriceLookup.Wire(), tool: "FetchUrl"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("delegates"));
    }
}
