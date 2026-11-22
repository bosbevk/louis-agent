using LouisAgent.Api.Sessions;
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
/// only minions can't", and "a cook can't call a cook. but it could call a recipe specialist"; on loops, no agent twice
/// in a chain of asks and a depth cap as a setting. A session's level comes from its role's folder below the top.
/// The roles are the test agent's (F166.1): its specialist stands for the cook, its reader for the recipe reader.
/// </summary>
public class AskDepthTests
{
    /// <summary>The specialist's role file, which the chain names it by.</summary>
    private const string Specialist = TestAgent.Specialist;

    /// <summary>The test agent's name, which the chain names its voice by.</summary>
    private const string Agent = TestAgent.Name;

    /// <summary>Fresh sessions on the test agent's catalogue, as <c>AgentSessionsTests</c> opens them.</summary>
    private static AgentSessions Fresh(int maxDepth = AgentOptions.DefaultAgentMaxDepth) =>
        AgentSessionsTests.Sessions(maxDepth: maxDepth);

    /// <summary>
    /// A specialist at any depth may load the agents toolset; a minion never, not even marked optional.
    /// </summary>
    [Test]
    public void EveryoneButAMinion_MayLoadTheAgentsToolset()
    {
        var sessions = Fresh();
        string agents = CoreToolset.Agents.Wire();

        var specialist = sessions.Create([agents, TestToolset.Web.Wire()], depth: 1, role: Specialist);
        var reader = sessions.Create([agents], depth: 2, role: TestAgent.Reader);

        Assert.Multiple(() =>
        {
            Assert.That(specialist.Toolsets, Does.Contain(agents));
            Assert.That(reader.Toolsets, Does.Contain(agents));
            foreach (string asked in new[] { agents, agents + ToolsetCatalog.OptionalMark })
                Assert.That(
                    () => sessions.Create([asked], ParentOf(specialist), depth: 2, role: TestAgent.Lookup,
                        tool: "FetchUrl"),
                    Throws.TypeOf<ToolsetException>().With.Message.Contain("asks no one"));
        });
    }

    /// <summary>
    /// Below the top a session opens at its role's own level: a voice may hand a minion one of its tools at depth 1,
    /// and a specialist asked by a specialist opens as one at depth 2.
    /// </summary>
    [Test]
    public void BelowTheTop_TheLevelIsTheRoles()
    {
        var sessions = Fresh();
        var voice = sessions.Create([TestToolset.Web.Wire()], role: TestAgent.Voice);

        var minion = sessions.Create(null, ParentOf(voice), depth: 1, role: TestAgent.Lookup, tool: "FetchUrl");
        var specialist = sessions.Create([TestToolset.Web.Wire()], depth: 2, role: Specialist);

        Assert.Multiple(() =>
        {
            Assert.That((minion.Depth, minion.Tool), Is.EqualTo((1, "FetchUrl")));
            Assert.That(minion.Engine.SkillDocumentation, Does.Contain(TestAgent.LookupHeading));
            Assert.That((specialist.Depth, specialist.Role, specialist.Tool),
                Is.EqualTo((2, Specialist, (string?)null)));
        });
    }

    /// <summary>The deepest session is AGENT_MAX_DEPTH's, 4 unless an instance sets it.</summary>
    [Test]
    public void TheDeepestSession_IsTheSettings()
    {
        Assert.That(Fresh().Create([TestToolset.Files.Wire()], depth: 4).Depth, Is.EqualTo(4));
        Assert.That(() => Fresh().Create([TestToolset.Files.Wire()], depth: 5),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("between 0 and 4"));
        Assert.That(() => Fresh(maxDepth: 2).Create([TestToolset.Files.Wire()], depth: 3),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("between 0 and 2"));
    }

    /// <summary>
    /// A session keeps the chain it was asked through, and an agent already in it is refused: the specialist under
    /// itself, or the agent under the specialist it asked; the reader under the specialist opens, and its turn runs
    /// inside the chain.
    /// </summary>
    [Test]
    public void AnAgentAlreadyInTheChain_IsRefused_AndATurnRunsInsideItsChain()
    {
        var sessions = Fresh();
        var voice = sessions.Create([TestToolset.Web.Wire()], role: TestAgent.Voice);
        var specialist = sessions.Create([TestToolset.Web.Wire()], ParentOf(voice), depth: 1, role: Specialist);

        var reader = sessions.Create([TestToolset.Files.Wire()], ParentOf(specialist), depth: 2,
            role: TestAgent.Reader);

        Assert.Multiple(() =>
        {
            Assert.That(reader.Chain, Is.EqualTo(new[] { Agent, Specialist, TestAgent.Reader }));
            Assert.That(
                () => sessions.Create([TestToolset.Web.Wire()], ParentOf(specialist), depth: 2, role: Specialist),
                Throws.TypeOf<RoleException>().With.Message.Contain(Specialist));
            Assert.That(() => sessions.Create(null, ParentOf(specialist), depth: 2, identity: Agent),
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
