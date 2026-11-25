using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Opening;
using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Api.Testing;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Usage.Records;
using static LouisAgent.Api.Tests.Sessions.AgentSessionsTests;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// Who may ask whom, on the API (F127.5). Louis: "everyone should be able to call everyone, if it is setup that way
/// only lookup agents can't", and "a cook can't call a cook. but it could call a recipe subagent"; on loops, no agent
/// twice in a chain of asks and a depth cap as a setting. A session's level comes from its role's folder below the top.
/// The roles are the test agent's (F166.1): its subagent stands for the cook, its reader for the recipe reader.
/// </summary>
public class AskDepthTests
{
    /// <summary>The subagent's role file, which the chain names it by.</summary>
    private const string Researcher = TestAgent.Researcher;

    /// <summary>The test agent's name, which the chain names its voice by.</summary>
    private const string Agent = TestAgent.Name;

    /// <summary>Fresh sessions on the test agent's catalogue, as <c>AgentSessionsTests</c> opens them.</summary>
    private static AgentSessions Fresh(int maxDepth = AgentOptions.DefaultAgentMaxDepth) =>
        AgentSessionsTests.Sessions(maxDepth: maxDepth);

    /// <summary>
    /// A subagent at any depth may load the agents toolset; a lookup agent never, not even marked optional.
    /// </summary>
    [Test]
    public async Task EveryoneButALookupAgent_MayLoadTheAgentsToolset()
    {
        var sessions = Fresh();
        string agents = CoreToolset.Agents.Wire();

        var subagent =
            (await sessions.CreateAsync(
                new SessionRequest([agents, TestToolset.Web.Wire()], Depth: 1, Role: Researcher)));
        var reader = (await sessions.CreateAsync(new SessionRequest([agents], Depth: 2, Role: TestAgent.Reader)));

        Assert.Multiple(() =>
        {
            Assert.That(subagent.Toolsets, Does.Contain(agents));
            Assert.That(reader.Toolsets, Does.Contain(agents));
            foreach (string asked in new[] { agents, agents + ToolsetCatalog.OptionalMark })
                Assert.That(
                    async () => (await sessions.CreateAsync(new SessionRequest([asked], ParentOf(subagent), Depth: 2,
                        Role: TestAgent.Lookup,
                        Tool: "FetchUrl"))),
                    Throws.TypeOf<ToolsetException>().With.Message.Contain("asks no one"));
        });
    }

    /// <summary>
    /// Below the top a session opens at its role's own level: a voice may hand a lookup agent one of its tools at depth
    /// 1, and a subagent asked by a subagent opens as one at depth 2.
    /// </summary>
    [Test]
    public async Task BelowTheTop_TheLevelIsTheRoles()
    {
        var sessions = Fresh();
        var voice = (await sessions.CreateAsync(new SessionRequest([TestToolset.Web.Wire()], Role: TestAgent.Voice)));

        var lookup = (await sessions.CreateAsync(new SessionRequest(null, ParentOf(voice), Depth: 1,
            Role: TestAgent.Lookup,
            Tool: "FetchUrl")));
        var subagent =
            (await sessions.CreateAsync(new SessionRequest([TestToolset.Web.Wire()], Depth: 2, Role: Researcher)));

        Assert.Multiple(() =>
        {
            Assert.That((lookup.Depth, lookup.Tool), Is.EqualTo((1, "FetchUrl")));
            Assert.That(lookup.Engine.SkillDocumentation, Does.Contain(TestAgent.LookupHeading));
            Assert.That((subagent.Depth, subagent.Role, subagent.Tool),
                Is.EqualTo((2, Researcher, (string?)null)));
        });
    }

    /// <summary>The deepest session is AGENT_MAX_DEPTH's, 4 unless an instance sets it.</summary>
    [Test]
    public async Task TheDeepestSession_IsTheSettings()
    {
        Assert.That((await Fresh().CreateAsync(new SessionRequest([TestToolset.Files.Wire()], Depth: 4))).Depth,
            Is.EqualTo(4));
        Assert.That(async () => (await Fresh().CreateAsync(new SessionRequest([TestToolset.Files.Wire()], Depth: 5))),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("between 0 and 4"));
        Assert.That(
            async () => (await Fresh(maxDepth: 2)
                .CreateAsync(new SessionRequest([TestToolset.Files.Wire()], Depth: 3))),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("between 0 and 2"));
    }

    /// <summary>
    /// A session keeps the chain it was asked through, and an agent already in it is refused: the subagent under
    /// itself, or the agent under the subagent it asked; the reader under the subagent opens, and its turn runs
    /// inside the chain.
    /// </summary>
    [Test]
    public async Task AnAgentAlreadyInTheChain_IsRefused_AndATurnRunsInsideItsChain()
    {
        var sessions = Fresh();
        var voice = (await sessions.CreateAsync(new SessionRequest([TestToolset.Web.Wire()], Role: TestAgent.Voice)));
        var subagent =
            (await sessions.CreateAsync(new SessionRequest([TestToolset.Web.Wire()], ParentOf(voice), Depth: 1,
                Role: Researcher)));

        var reader = (await sessions.CreateAsync(new SessionRequest([TestToolset.Files.Wire()], ParentOf(subagent),
            Depth: 2,
            Role: TestAgent.Reader)));

        Assert.Multiple(() =>
        {
            Assert.That(reader.Chain, Is.EqualTo(new[] { Agent, Researcher, TestAgent.Reader }));
            Assert.That(
                async () => (await sessions.CreateAsync(new SessionRequest([TestToolset.Web.Wire()], ParentOf(subagent),
                    Depth: 2,
                    Role: Researcher))),
                Throws.TypeOf<RoleException>().With.Message.Contain(Researcher));
            Assert.That(
                async () => (await sessions.CreateAsync(new SessionRequest(null, ParentOf(subagent), Depth: 2,
                    Identity: Agent))),
                Throws.TypeOf<RoleException>().With.Message.Contain(Agent));
        });

        using var usage = UsageScope.Begin(session: reader.Id, turn: 1);
        using (TurnAmbience.Enter(reader, usage, _ => { }))
        {
            Assert.That(TurnChain.Current, Is.EqualTo(reader.Chain));
            Assert.That(TurnDepth.Current, Is.EqualTo(2));
        }
    }
}
