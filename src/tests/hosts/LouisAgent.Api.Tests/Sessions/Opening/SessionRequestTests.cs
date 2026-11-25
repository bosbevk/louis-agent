using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Opening;

namespace LouisAgent.Api.Tests.Sessions.Opening;

/// <summary>
/// A session is asked for with one record (CL70, Fowler's Introduce Parameter Object): naming nothing opens today's
/// default session, and a caller names only what it sets.
/// </summary>
public class SessionRequestTests
{
    /// <summary>An empty request is the default: the skills alone, at the top, in the default voice.</summary>
    [Test]
    public async Task AnEmptyRequest_OpensTheDefaultSession()
    {
        var session = (await AgentSessionsTests.Sessions().CreateAsync(new SessionRequest()));

        Assert.Multiple(() =>
        {
            Assert.That(session.Toolsets, Is.EqualTo(AgentSessions.DefaultToolsets));
            Assert.That(session.Depth, Is.Zero);
            Assert.That(session.Tool, Is.Null);
        });
    }
}
