using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Choices;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// FX134: the pages of the agents' windows on the visitor's screen come with their message, and the turn runs with
/// them, so an ask to a business whose window is there sends it its page.
/// </summary>
public class WindowPagesTests
{
    /// <summary>The windows' pages a message brought are the turn's, for the agent each window shows.</summary>
    [Test]
    public void AMessagesWindowPages_AreTheTurns()
    {
        var session = AgentSessionsTests.Sessions().Create([TestToolset.Files.Wire()]);
        var page = new PageRequest("https://acme.example/", "Acme", "Four rooms.");

        session.Windows = [new WindowPageRequest("acme", page)];

        using var usage = UsageScope.Begin(session: session.Id, turn: 1);
        using (TurnAmbience.Enter(session, usage, _ => { }))
        {
            Assert.That(WindowPages.For("acme"), Is.EqualTo(page));
            Assert.That(WindowPages.For("other"), Is.Null);
        }
    }
}
