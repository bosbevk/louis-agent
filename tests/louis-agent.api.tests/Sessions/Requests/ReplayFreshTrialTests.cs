using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Api.Tests.Sessions.Requests;

// F18 step 5a: a trial the runner runs twice (a transport failure, one retry) records under the same key both times, and the replay store answers an ask from
// its first recording. So a top-level eval session that records starts its trial afresh. A sub-session records into its parent's trial (it is sent the
// parent's `of`), and must never clear it. Written first: FreshTrial did not exist.
public class ReplayFreshTrialTests
{
    private static CreateSessionRequest Request(ReplayRequest replay) =>
        new("bobotie", "run_1:puls-cook:1", null, Replay: replay);

    private static string? Fresh(ReplayRequest sent, bool resolved = true)
    {
        var request = Request(sent);
        ReplayResolution.TryResolve(request, WebReplayMode.Off, keepsRecordings: true, out var replay, out _);
        return ReplayResolution.FreshTrial(request, resolved ? replay : null);
    }

    [Test]
    public void ARecordingEvalSession_StartsItsOwnTrialAfresh() =>
        Assert.That(Fresh(new ReplayRequest("record", null)), Is.EqualTo("run_1:puls-cook:1/bobotie"));

    [Test]
    public void ASubSession_SentItsParentsTrial_NeverClearsIt() =>
        Assert.That(Fresh(new ReplayRequest("record", "run_1:puls-cook:1/bobotie")), Is.Null);

    [TestCase("replay")]
    [TestCase("off")]
    public void OnlyARecordingStartsAfresh(string web) =>
        Assert.That(Fresh(new ReplayRequest(web, web == "replay" ? "run_0:puls-cook:1/bobotie" : null)), Is.Null);

    [Test]
    public void ASessionWithNoReplay_StartsNothing() =>
        Assert.That(Fresh(new ReplayRequest("record", null), resolved: false), Is.Null);
}
