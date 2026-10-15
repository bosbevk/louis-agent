using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests;

// F5 step 6 (F5-S1): a session chooses its toolsets when it opens, and keeps them; sessions with different lists don't
// share tools. No model, network or web host: the sessions are built on a factory with a client that is never called.
public class AgentSessionsTests
{
    private static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    private static AgentSessions Sessions() => new(new AgentEngineFactory(new NoModel(),
        new AgentOptions { WorkspaceRoot = RepoRoot, SkillsDirectory = Path.Combine(RepoRoot, "Skills"), RatesUrl = null }));

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
    public void ASubSession_CantLoadAgents_AndDepthIsOneLevel()
    {
        var sessions = Sessions();

        Assert.That(() => sessions.Create([AgentSessions.AgentsToolset], depth: 1),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("one level deep"));
        Assert.That(() => sessions.Create(["files"], depth: 2), Throws.TypeOf<ToolsetException>());
        Assert.That(sessions.Create(["files"], depth: 1).Depth, Is.EqualTo(1));
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
    public void ASubSessionWithoutARole_GetsOneFromItsToolsets_AndAGivenOneIsKept()
    {
        // The delegation check's option 4: the role in code, on the peer, not left to the parent's prompt.
        var sessions = Sessions();

        Assert.That(sessions.Create(["web"], depth: 1).Personality, Is.EqualTo("puls-researcher"));
        Assert.That(sessions.Create(["files", "python"], depth: 1).Personality, Is.EqualTo("puls-analyst"));
        Assert.That(sessions.Create(["web"], depth: 1, personality: "puls-specialist").Personality, Is.EqualTo("puls-specialist"));
        Assert.That(sessions.Create(["web"]).Personality, Is.Null, "a top-level session keeps personality.md");
    }
}
