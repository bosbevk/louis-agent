using LouisAgent.Web.Storage;
using LouisAgent.Web.Testing;

namespace LouisAgent.Web.Tests.Api;

/// <summary>
/// F156, Louis's pick A of three: when a turn's answer says the server paced its words (<c>Stream-Paced: on</c>), the
/// client tells its caller once, before the first event, so the page can leave its own pacer off; without the header
/// it says nothing.
/// </summary>
public class AgentApiStreamPacedTests
{
    /// <summary>How many times the client said the server paced one turn, answered with the header or without.</summary>
    private static async Task<int> CountPacedCallsAsync(bool paced)
    {
        var api = FixtureApi.Client(new ChatSettings(), out _, paced: paced);
        int told = 0;

        await foreach (var _ in api.StreamMessageAsync(FixtureApi.SessionId, "Hi", [],
                           onServerPaced: () => told++)) { }

        return told;
    }

    /// <summary>A paced answer is told once.</summary>
    [Test]
    public async Task APacedAnswer_IsToldOnce() =>
        Assert.That(await CountPacedCallsAsync(paced: true), Is.EqualTo(1));

    /// <summary>An answer without the header is not told at all.</summary>
    [Test]
    public async Task AnUnpacedAnswer_IsNotTold() =>
        Assert.That(await CountPacedCallsAsync(paced: false), Is.Zero);
}
