using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests;

// F5 step 6 (F5-S1): a session chooses its toolsets when it opens, and keeps them; sessions with different lists don't
// share tools. No model, network or web host: the sessions are built on a factory with a client that is never called.
public class AgentSessionsTests
{
    private static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    private static AgentSessions Sessions(IReadOnlyList<SessionLlm>? llms = null) => new(new AgentEngineFactory(new NoModel(),
        new AgentOptions { WorkspaceRoot = RepoRoot, SkillsDirectory = Path.Combine(RepoRoot, "Skills"), RatesUrl = null },
        sessionLlms: llms));

    private static string[] Names(AgentSession session) => session.Engine.Tools.Select(t => t.Name).ToArray();

    [Test]
    public void ASessionWithFiles_HasNoGitTools()
    {
        var session = Sessions().Create(["files"]);

        Assert.That(session.Toolsets, Is.EqualTo(new[] { "files" }));
        Assert.That(Names(session), Does.Contain("ReadWorkspaceFile").And.Not.Contain("GetStatus"));
    }

    [Test]
    public void NoToolsets_GivesTheDefault_SkillsOnly()
    {
        var session = Sessions().Create();

        Assert.That(session.Toolsets, Is.EqualTo(AgentSessions.DefaultToolsets));
        Assert.That(Names(session), Does.Contain("CreateTool").And.Not.Contain("ReadWorkspaceFile"));
    }

    [Test]
    public void AnEmptyList_GivesNoToolsets()
    {
        Assert.That(Names(Sessions().Create([])), Is.EquivalentTo(new[] { "ExecuteSkill", "GetToday" }));
    }

    [Test]
    public void TwoSessionsWithDifferentLists_DontShareTools_TwoWithTheSameShareAnEngine()
    {
        var sessions = Sessions();

        var files = sessions.Create(["files"]);
        var git = sessions.Create(["git"]);
        var filesAgain = sessions.Create(["FILES"]);

        Assert.That(git.Engine, Is.Not.SameAs(files.Engine));
        Assert.That(Names(git), Does.Not.Contain("ReadWorkspaceFile"));
        Assert.That(filesAgain.Engine, Is.SameAs(files.Engine), "one engine, one cacheable prefix per profile");
        Assert.That(filesAgain.History, Is.Not.SameAs(files.History), "but each session its own history");
    }

    [Test]
    public void AnUnknownToolset_IsRefused_WithTheValidNames()
    {
        Assert.That(() => Sessions().Create(["files", "nope"]),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("Valid names").And.Message.Contain("files"));
    }

    [Test]
    public void ASubSession_CantLoadSpecialists_AndDepthStopsAtTheMinion()
    {
        var sessions = Sessions();

        Assert.That(() => sessions.Create(["specialists"], depth: 1),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("delegates"));
        Assert.That(() => sessions.Create(["files"], depth: 3), Throws.TypeOf<ToolsetException>().With.Message.Contain("between 0 and 2"));
        Assert.That(sessions.Create(["files"], depth: 1).Depth, Is.EqualTo(1));
        // CL9 step 5: a client still sending the old name gets the 400 that lists the valid toolsets.
        Assert.That(() => sessions.Create(["agents"]), Throws.TypeOf<ToolsetException>().With.Message.Contain("specialists"));
    }

    [Test]
    public void ASession_IsFoundById_UntilRemoved()
    {
        var sessions = Sessions();
        var session = sessions.Create(["files"]);

        Assert.That(sessions.Get(session.Id), Is.SameAs(session));
        Assert.That(sessions.Remove(session.Id), Is.True);
        Assert.That(sessions.Get(session.Id), Is.Null);
    }

    /// <summary>The sessions never prompt a model in these tests.</summary>
    private sealed class NoModel : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    [Test]
    public void ASubSession_HasNoRouter_ATopLevelOneHas()
    {
        // F21: default.md picks toolsets and delegates; a sub-session's toolsets were chosen for it and it can't delegate.
        var sessions = Sessions();

        var top = sessions.Create(["files"]);
        var sub = sessions.Create(["files"], depth: 1);

        Assert.That(top.Engine.SkillDocumentation, Does.Contain("# Louis Agent - Default Instructions"));
        Assert.That(sub.Engine.SkillDocumentation, Does.Not.Contain("# Louis Agent - Default Instructions"));
        Assert.That(sub.Engine.SkillDocumentation, Does.Contain("# Files Skills"), "its toolsets' guides stay");
    }

    [Test]
    public void AnUnknownPersonality_IsRefused()
    {
        Assert.Throws<LouisAgent.Core.Skills.PersonalityException>(() => Sessions().Create(["files"], personality: "nobody-here"));
    }

    [Test]
    public void ARole_OpensOnlyAtItsOwnLevel()
    {
        // CL8: a session's level is its depth; the cook is a specialist (level 1), Puls's voice is level 0's.
        var sessions = Sessions();

        Assert.Throws<LouisAgent.Core.Skills.PersonalityException>(() => sessions.Create(["web"], personality: "puls-cook"));
        Assert.Throws<LouisAgent.Core.Skills.PersonalityException>(() => sessions.Create(["web"], depth: 1, personality: "puls/voice-af"));
        Assert.That(sessions.Create(["web"], personality: "puls/voice-af").Personality, Is.EqualTo("puls/voice-af"));
        Assert.That(sessions.Create(["web"], depth: 1, personality: "puls-cook").Personality, Is.EqualTo("puls-cook"));
    }

    [Test]
    public void NoPersonality_IsTheDefault()
    {
        var session = Sessions().Create(["files"], personality: "");

        Assert.That(session.Personality, Is.Null);
        Assert.That(session.Engine.SkillDocumentation, Does.Contain("Louis's Minion"));
    }

    [Test]
    public void ASession_RemembersItsAttachments_ByName_Capped()
    {
        // F22: a later turn can still hand an earlier attachment to a specialist.
        var session = Sessions().Create(["files"]);

        session.Remember("Verslag.txt", "eerste");
        session.Remember("verslag.txt", "tweede");
        session.Remember("groot.txt", new string('x', AgentSession.MaxAttachmentChars + 10));

        Assert.That(session.Attachments["VERSLAG.TXT"], Is.EqualTo("tweede"), "names ignore case; the newest wins");
        Assert.That(session.Attachments["groot.txt"], Has.Length.EqualTo(AgentSession.MaxAttachmentChars));
    }

    [Test]
    public void ThePlace_HasTheLocationWhenShared_AndTheTimeZoneAlways()
    {
        // F22 step 2b, and PR #28's review: a time zone sent without a location is kept (GetToday in the user's time).
        Assert.That(new CreateSessionRequest(null, null, null, Location: " Pretoria ", TimeZone: "Africa/Johannesburg").Place(),
            Is.EqualTo(new LouisAgent.Core.Tools.Clock.UserPlace("Pretoria", "Africa/Johannesburg")));
        Assert.That(new CreateSessionRequest(null, null, null, TimeZone: "Europe/London").Place(),
            Is.EqualTo(new LouisAgent.Core.Tools.Clock.UserPlace(null, "Europe/London")));
        Assert.That(new CreateSessionRequest(null, null, null).Place(), Is.Null);

        var session = Sessions().Create(["files"], place: new LouisAgent.Core.Tools.Clock.UserPlace("Pretoria", null));
        Assert.That(session.Place!.Location, Is.EqualTo("Pretoria"));
    }

    private static LouisAgent.Core.Tools.Agents.OpenSubSession Kept(string sessionId, List<string> closed) =>
        new(new Uri("http://puls-agent/"), sessionId, "puls-agent", ["web"], "puls-cook", () => { closed.Add(sessionId); return Task.CompletedTask; });

    [Test]
    public async Task RemovingASession_ClosesTheSubSessionsItKept()
    {
        // F26 step 3 (F26-S3): the chat is deleted, so its specialists are deleted on the peer too.
        var sessions = Sessions();
        var session = sessions.Create(["files"]);   // the test factory has no peers; the registry doesn't care
        var closed = new List<string>();
        await session.SubSessions.AddAsync(Kept("sess_cook", closed));

        Assert.That(sessions.Remove(session.Id), Is.True);

        Assert.That(closed, Is.EqualTo(new[] { "sess_cook" }));
        Assert.That(session.SubSessions.Open, Is.Empty);
    }

    [Test]
    public async Task AnIdleSession_IsDropped_WithItsSubSessions()
    {
        var sessions = Sessions();
        var session = sessions.Create(["files"]);
        var closed = new List<string>();
        await session.SubSessions.AddAsync(Kept("sess_cook", closed));

        int dropped = sessions.DropIdle(DateTimeOffset.UtcNow + AgentSessions.IdleTimeout + TimeSpan.FromMinutes(1));

        Assert.That(dropped, Is.EqualTo(1));
        Assert.That(sessions.Get(session.Id), Is.Null);
        Assert.That(closed, Is.EqualTo(new[] { "sess_cook" }));
    }

    [Test]
    public void ASubSessionWithoutARole_GetsNone_AndAGivenOneIsKept()
    {
        // CL10: a role is named by the parent, never derived from the toolsets; with none, the default mode's voice only.
        var sessions = Sessions();

        var unnamed = sessions.Create(["web"], depth: 1);
        Assert.That(unnamed.Personality, Is.Null);
        Assert.That(unnamed.Engine.SkillDocumentation, Does.Contain("Louis's Minion").And.Not.Contain("# Louis Agent - Default Instructions"));
        Assert.That(sessions.Create(["web"], depth: 1, personality: "puls-cook").Personality, Is.EqualTo("puls-cook"));
        Assert.That(sessions.Create(["web"]).Personality, Is.Null, "a top-level session that names none gets the default voice");
    }

    [Test]
    public void ASessionNamingAnLlmProfile_KeepsIt_AndOneWithout_HasNone()
    {
        // F30: a price lookup runs on the local model, the chat that asked for it on the instance's.
            var sessions = Sessions([new SessionLlm("ollama-puls", new NoModel())]);

        var local = sessions.Create(["web"], depth: 1, llm: "ollama-puls");
        var own = sessions.Create(["web"], depth: 1);

        Assert.That(local.Llm, Is.EqualTo("ollama-puls"));
        Assert.That(own.Llm, Is.Null);
        Assert.That(local.Engine, Is.Not.SameAs(own.Engine), "one engine per model");
    }

    [Test]
    public void AnUnknownLlmProfile_IsRefused_NamingTheOnesThereAre()
    {
        var sessions = Sessions([new SessionLlm("ollama-puls", new NoModel())]);

        var ex = Assert.Throws<SessionLlmException>(() => sessions.Create(["web"], llm: "gpt-9"));
        Assert.That(ex!.Message, Does.Contain("gpt-9").And.Contain("ollama-puls"));
    }

    private static UsageTags ParentOf(AgentSession parent) => new(null, null, null, $"{parent.Id}#1");

    [Test]
    public void AMinion_OpensAtDepthTwo_WithOneToolItsParentHas()
    {
        // F30 step 4: a specialist hands a minion one task and one of its own tools; the peer checks the tool against
        // the parent session it finds here (minions run on the specialist's own instance).
        var sessions = Sessions();
        var cook = sessions.Create(["web"], depth: 1, personality: "puls-cook");

        var minion = sessions.Create(["web"], ParentOf(cook), depth: 2, personality: "puls-minion-web-price-lookup", tool: "FetchUrl");

        Assert.That(minion.Depth, Is.EqualTo(2));
        Assert.That(minion.Tool, Is.EqualTo("FetchUrl"));
        Assert.That(Names(minion), Is.EqualTo(new[] { "FetchUrl" }));
        Assert.That(minion.Engine.SkillDocumentation, Does.Contain("# Puls minion: Web price lookup").And.Not.Contain("# Web Skills"));
        Assert.That(cook.Tool, Is.Null, "a specialist has toolsets, not one tool");
    }

    [Test]
    public void AMinion_GetsOnlyAToolItsParentHas_AndOneThatIsEverHandedDown()
    {
        // Capability delegation: a minion can never do more than the specialist that sent it. The parent's toolbox is
        // the only list (Louis: "the specialist must give him its tools"); delegation and self-extension never go down.
        var sessions = Sessions();
        var withFiles = sessions.Create(["files"], depth: 1, personality: "puls-cook");
        var withWeb = sessions.Create(["web", "files"], depth: 1, personality: "puls-cook");

        Assert.That(() => sessions.Create(["web"], ParentOf(withFiles), depth: 2, personality: "puls-minion-web-price-lookup", tool: "FetchUrl"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("parent session has no 'FetchUrl'"));
        Assert.That(sessions.Create(["files"], ParentOf(withWeb), depth: 2, personality: "puls-minion-web-price-lookup", tool: "ReadWorkspaceFile").Tool,
            Is.EqualTo("ReadWorkspaceFile"), "any tool of the parent's own: which one is the specialist's call");
        Assert.That(() => sessions.Create(["skills"], ParentOf(withWeb), depth: 2, personality: "puls-minion-web-price-lookup", tool: "ExecuteSkill"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("never handed"));
    }

    [Test]
    public void AMinion_NeedsAParentItCanFind_AMinionRole_AndATool_AndNothingDeeper()
    {
        var sessions = Sessions();
        var cook = sessions.Create(["web"], depth: 1, personality: "puls-cook");
        var parent = ParentOf(cook);

        Assert.That(() => sessions.Create(["web"], depth: 2, personality: "puls-minion-web-price-lookup", tool: "FetchUrl"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("parent"), "no parent");
        Assert.That(() => sessions.Create(["web"], new UsageTags(null, null, null, "sess_gone#1"), depth: 2, personality: "puls-minion-web-price-lookup", tool: "FetchUrl"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("sess_gone"), "a parent it can't find");
        Assert.That(() => sessions.Create(["web"], parent, depth: 2, personality: "puls-cook", tool: "FetchUrl"),
            Throws.TypeOf<LouisAgent.Core.Skills.PersonalityException>(), "a specialist's role is no minion role");
        Assert.That(() => sessions.Create(["web"], parent, depth: 2, personality: "puls-minion", tool: "FetchUrl"),
            Throws.TypeOf<LouisAgent.Core.Skills.PersonalityException>(), "the base is no role");
        Assert.That(() => sessions.Create(["web"], parent, depth: 2, tool: "FetchUrl"),
            Throws.TypeOf<LouisAgent.Core.Skills.PersonalityException>(), "a minion always has a role");
        Assert.That(() => sessions.Create(["web"], parent, depth: 2, personality: "puls-minion-web-price-lookup"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("'tool'"), "a minion always has its one tool");
        Assert.That(() => sessions.Create(["web"], depth: 1, personality: "puls-cook", tool: "FetchUrl"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("'tool'"), "a specialist loads toolsets, not a tool");
        Assert.That(() => sessions.Create(["web"], parent, depth: 3, personality: "puls-minion-web-price-lookup", tool: "FetchUrl"),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("between 0 and 2"), "a minion never delegates");
    }
}
