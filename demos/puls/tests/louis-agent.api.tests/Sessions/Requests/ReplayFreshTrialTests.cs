using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Api.Tests.Sessions.Requests;

/// <summary>
/// A trial the runner runs twice (a transport failure, one retry) records under the same key both times, and the
/// replay store answers an ask from its first recording (F18 step 5a). So a top-level eval session that records starts
/// its trial afresh. A sub-session records into its parent's trial (it is sent the parent's <c>of</c>) and must never
/// clear it. Written first: <c>FreshTrial</c> did not exist.
/// </summary>
public class ReplayFreshTrialTests
{
    /// <summary>An eval's opening with a run and a task, sending this <c>replay</c>.</summary>
    private static CreateSessionRequest Request(ReplayRequest replay) =>
        new("bobotie", "run_1:puls-cook:1", null, Replay: replay);

    /// <summary>The trial this opening clears first, if any, as the API works it out.</summary>
    private static string? Fresh(ReplayRequest sent, bool resolved = true)
    {
        var request = Request(sent);
        ReplayResolution.TryResolve(request, WebReplayMode.Off, keepsRecordings: true, out var replay, out _);
        return ReplayResolution.FreshTrial(request, resolved ? replay : null);
    }

    /// <summary>A recording eval session clears its own trial before it starts.</summary>
    [Test]
    public void ARecordingEvalSession_StartsItsOwnTrialAfresh() =>
        Assert.That(Fresh(new ReplayRequest("record", null)), Is.EqualTo("run_1:puls-cook:1/bobotie"));

    /// <summary>A sub-session records into its parent's trial and never clears it.</summary>
    [Test]
    public void ASubSession_SentItsParentsTrial_NeverClearsIt() =>
        Assert.That(Fresh(new ReplayRequest("record", "run_1:puls-cook:1/bobotie")), Is.Null);

    /// <summary>Replaying, or not using the recordings at all, clears nothing.</summary>
    [TestCase("replay")]
    [TestCase("off")]
    public void OnlyARecordingStartsAfresh(string web) =>
        Assert.That(Fresh(new ReplayRequest(web, web == "replay" ? "run_0:puls-cook:1/bobotie" : null)), Is.Null);

    /// <summary>A session that ends up with no replay clears nothing.</summary>
    [Test]
    public void ASessionWithNoReplay_StartsNothing() =>
        Assert.That(Fresh(new ReplayRequest("record", null), resolved: false), Is.Null);
}
