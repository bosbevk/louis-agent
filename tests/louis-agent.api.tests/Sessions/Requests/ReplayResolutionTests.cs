using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Api.Tests.Sessions.Requests;

// F18 step 4a.2: what an eval session does with the web is decided here, never by the runner (ADR 0029): one test per rule, in the
// order the rules apply. Written first: TryResolve threw NotImplementedException on the red run.
public class ReplayResolutionTests
{
    private const string Run = "run_1:puls-cook:1";

    private static CreateSessionRequest Request(string? run = Run, string? task = "bobotie", ReplayRequest? replay = null) =>
        new(task, run, null, Replay: replay);

    private static (bool Ok, SessionReplay? Replay, string? Refusal) Resolve(CreateSessionRequest? request, WebReplayMode defaultMode = WebReplayMode.Off,
        bool keepsRecordings = true)
    {
        bool ok = ReplayResolution.TryResolve(request, defaultMode, keepsRecordings, out var replay, out string? refusal);
        return (ok, replay, refusal);
    }

    // 1. No run tag: a live chat is never recorded, and the default never applies.
    [TestCase(WebReplayMode.Off)]
    [TestCase(WebReplayMode.Record)]
    [TestCase(WebReplayMode.Replay)]
    public void NoRunTag_GivesNoReplay_WhateverTheDefault(WebReplayMode defaultMode)
    {
        Assert.That(Resolve(Request(run: null), defaultMode), Is.EqualTo((true, (SessionReplay?)null, (string?)null)));
        Assert.That(Resolve(null, defaultMode), Is.EqualTo((true, (SessionReplay?)null, (string?)null)));
        Assert.That(Resolve(Request(run: "  "), defaultMode).Replay, Is.Null);
    }

    [Test]
    public void AReplaySentWithNoRunTag_IsRefused()
    {
        var (ok, replay, refusal) = Resolve(Request(run: null, replay: new ReplayRequest("record", null)));

        Assert.That((ok, replay), Is.EqualTo((false, (SessionReplay?)null)));
        Assert.That(refusal, Does.Contain("replay").And.Contain("run"));
    }

    // 2. A run tag and no replay: the default.
    [Test]
    public void ARunTagAndNoReplay_IsTheDefault_OffGivesNone()
    {
        Assert.That(Resolve(Request(), WebReplayMode.Off), Is.EqualTo((true, (SessionReplay?)null, (string?)null)));
    }

    [Test]
    public void TheDefaultRecord_WithARunTag_RecordsIntoRunSlashTask()
    {
        var (ok, replay, _) = Resolve(Request(), WebReplayMode.Record);

        Assert.That((ok, replay), Is.EqualTo((true, new SessionReplay(WebReplayMode.Record, $"{Run}/bobotie", false))));
    }

    [Test]
    public void ARecordingWithNoTask_UsesTheRunTagAlone()
    {
        Assert.That(Resolve(Request(task: null), WebReplayMode.Record).Replay, Is.EqualTo(new SessionReplay(WebReplayMode.Record, Run, false)));
        Assert.That(Resolve(Request(task: " "), WebReplayMode.Record).Replay!.Of, Is.EqualTo(Run));
    }

    [Test]
    public void TheDefaultReplay_NeedsAnOf_SoItIsRefused()
    {
        var (ok, _, refusal) = Resolve(Request(), WebReplayMode.Replay);

        Assert.That(ok, Is.False);
        Assert.That(refusal, Does.Contain("needs 'of'"));
    }

    // 3. web is parsed with SettingChoice.
    [Test]
    public void WebOff_GivesNone_EvenWhereTheDefaultRecords()
    {
        Assert.That(Resolve(Request(replay: new ReplayRequest("off", null)), WebReplayMode.Record), Is.EqualTo((true, (SessionReplay?)null, (string?)null)));
    }

    [TestCase("record", WebReplayMode.Record)]
    [TestCase("RECORD", WebReplayMode.Record)]
    [TestCase(" Replay ", WebReplayMode.Replay)]
    public void WebIsParsedWithTheCaseIgnored(string web, WebReplayMode expected) =>
        Assert.That(Resolve(Request(replay: new ReplayRequest(web, "run_0:puls-cook:1/bobotie"))).Replay!.Web, Is.EqualTo(expected));

    [TestCase("sometimes")]
    [TestCase("1")]
    public void AnUnknownWeb_IsRefused_ListingTheSupportedNames(string web)
    {
        var (ok, _, refusal) = Resolve(Request(replay: new ReplayRequest(web, null)));

        Assert.That(ok, Is.False);
        Assert.That(refusal, Does.Contain(web).And.Contain("off, record, replay"));
    }

    [Test]
    public void AReplayWithNoWeb_UsesTheInstanceDefault()
    {
        Assert.That(Resolve(Request(replay: new ReplayRequest(null, "run_0:puls-cook:1/bobotie")), WebReplayMode.Replay).Replay,
            Is.EqualTo(new SessionReplay(WebReplayMode.Replay, "run_0:puls-cook:1/bobotie", false)));
        Assert.That(Resolve(Request(replay: new ReplayRequest(null, null)), WebReplayMode.Off).Replay, Is.Null);
    }

    // 4. model: true is refused until step 4e, never pretended.
    [TestCase("record")]
    [TestCase("replay")]
    [TestCase("off")]
    public void ModelTrue_IsRefused_UntilStep4e(string web)
    {
        var (ok, replay, refusal) = Resolve(Request(replay: new ReplayRequest(web, "run_0:puls-cook:1/bobotie", Model: true)));

        Assert.That((ok, replay), Is.EqualTo((false, (SessionReplay?)null)));
        Assert.That(refusal, Does.Contain("replay.model").And.Contain("4e"));
    }

    // 5. record or replay without REPLAY_DIRECTORY.
    [Test]
    public void RecordOrReplayWithoutAReplayDirectory_IsRefused_ThisInstanceKeepsNoRecordings()
    {
        Assert.That(Resolve(Request(replay: new ReplayRequest("record", null)), keepsRecordings: false).Refusal, Does.Contain("keeps no recordings"));
        Assert.That(Resolve(Request(replay: new ReplayRequest("replay", "x")), keepsRecordings: false).Refusal, Does.Contain("keeps no recordings"));
        Assert.That(Resolve(Request(), WebReplayMode.Record, keepsRecordings: false).Refusal, Does.Contain("keeps no recordings"), "the default is held to it too");
    }

    [Test]
    public void OffNeedsNoReplayDirectory() =>
        Assert.That(Resolve(Request(replay: new ReplayRequest("off", null)), keepsRecordings: false), Is.EqualTo((true, (SessionReplay?)null, (string?)null)));

    // 6. replay without of.
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void ReplayWithoutOf_IsRefused(string? of)
    {
        var (ok, _, refusal) = Resolve(Request(replay: new ReplayRequest("replay", of)));

        Assert.That(ok, Is.False);
        Assert.That(refusal, Does.Contain("needs 'of'"));
    }

    // 7. record without of is {run}/{task}; with of (a sub-session recording into its parent's trial) it is kept.
    [Test]
    public void RecordWithoutOf_IsRunSlashTask()
    {
        Assert.That(Resolve(Request(replay: new ReplayRequest("record", null))).Replay, Is.EqualTo(new SessionReplay(WebReplayMode.Record, $"{Run}/bobotie", false)));
    }

    [Test]
    public void ASubSessionsOf_IsKept_ItNeverWorksOutAKeyOfItsOwn()
    {
        // A sub-session's own task is sub-session:{name}, and one run tag holds every scenario of a mode and repeat.
        var request = Request(task: "sub-session:puls-cook", replay: new ReplayRequest("record", "run_1:puls-cook:1/bobotie"));

        Assert.That(Resolve(request).Replay, Is.EqualTo(new SessionReplay(WebReplayMode.Record, "run_1:puls-cook:1/bobotie", false)));
    }

    [Test]
    public void AReplayWithOf_IsKeptAsSent() =>
        Assert.That(Resolve(Request(replay: new ReplayRequest("replay", "run_0:puls-cook:1/bobotie"))).Replay,
            Is.EqualTo(new SessionReplay(WebReplayMode.Replay, "run_0:puls-cook:1/bobotie", false)));

    // The tags: replay.of is held to the length and the printable rule of the other tags.
    [Test]
    public void ReplayOf_IsATag_ShortAndPrintable()
    {
        Assert.That(Request(replay: new ReplayRequest("replay", new string('x', 200))).InvalidTag(), Is.Null);
        Assert.That(Request(replay: new ReplayRequest("replay", new string('x', 201))).InvalidTag(), Is.EqualTo("replay.of"));
        Assert.That(Request(replay: new ReplayRequest("replay", "a\nb")).InvalidTag(), Is.EqualTo("replay.of"));
        Assert.That(Request().InvalidTag(), Is.Null);
    }
}
