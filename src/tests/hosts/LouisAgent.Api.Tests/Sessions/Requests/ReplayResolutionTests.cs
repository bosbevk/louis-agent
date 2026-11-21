using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Api.Tests.Sessions.Requests;

/// <summary>
/// What an eval session does with the web is decided here, never by the runner (F18 step 4a.2, ADR 0029): one test per
/// rule, in the order the rules apply. Written first: <c>TryResolve</c> threw <c>NotImplementedException</c> on the red
/// run. From the review of 4a.2 (Louis, 2026-10-25): a run tag doesn't make a session an eval (the orchestrator sends
/// one on every fix session), so an eval marks itself by sending <c>replay</c>, even an empty <c>{}</c>, and the
/// instance default only fills in a missing <c>web</c>. The tests that send no <c>replay</c> expect none, whatever the
/// default, and were red against the rule that applied the default to every run-tagged session.
/// </summary>
public class ReplayResolutionTests
{
    /// <summary>The run tag every eval here carries.</summary>
    private const string Run = "run_1:puls-cook:1";

    /// <summary>
    /// An eval session's own mark: <c>replay: {}</c>, which takes the instance default for its web and nothing else.
    /// </summary>
    private static readonly ReplayRequest Eval = new(null, null);

    /// <summary>An opening with this run, task and <c>replay</c>.</summary>
    private static CreateSessionRequest Request(string? run = Run, string? task = "bobotie",
        ReplayRequest? replay = null) =>
        new(task, run, null, Replay: replay);

    /// <summary>What the API makes of an opening on an instance with this default and these recordings.</summary>
    private static (bool Ok, SessionReplay? Replay, string? Refusal) Resolve(CreateSessionRequest? request,
        WebReplayMode defaultMode = WebReplayMode.Off,
        bool keepsRecordings = true)
    {
        bool ok = ReplayResolution.TryResolve(request, defaultMode, keepsRecordings, out var replay,
            out string? refusal);
        return (ok, replay, refusal);
    }

    // Rule 1: no run tag.

    /// <summary>A live chat (no run tag) is never recorded, and the default never applies.</summary>
    [TestCase(WebReplayMode.Off)]
    [TestCase(WebReplayMode.Record)]
    [TestCase(WebReplayMode.Replay)]
    public void NoRunTag_GivesNoReplay_WhateverTheDefault(WebReplayMode defaultMode)
    {
        Assert.That(Resolve(Request(run: null), defaultMode), Is.EqualTo((true, (SessionReplay?)null, (string?)null)));
        Assert.That(Resolve(null, defaultMode), Is.EqualTo((true, (SessionReplay?)null, (string?)null)));
        Assert.That(Resolve(Request(run: "  "), defaultMode).Replay, Is.Null);
    }

    /// <summary>Asking to record or replay without a run tag is refused, saying so.</summary>
    [Test]
    public void AReplaySentWithNoRunTag_IsRefused()
    {
        var (ok, replay, refusal) = Resolve(Request(run: null, replay: new ReplayRequest("record", null)));

        Assert.That((ok, replay), Is.EqualTo((false, (SessionReplay?)null)));
        Assert.That(refusal, Does.Contain("replay").And.Contain("run"));
    }

    /// <summary>Even an empty <c>replay</c> needs a run tag: sending it is what makes the session an eval.</summary>
    [Test]
    public void AnEmptyReplayWithNoRunTag_IsRefusedToo_ItIsTheMarkThatCounts()
    {
        Assert.That(Resolve(Request(run: null, replay: Eval)).Ok, Is.False);
        Assert.That(Resolve(Request(run: "  ", replay: Eval), WebReplayMode.Record).Refusal, Does.Contain("run"));
    }

    // Rule 2: a run tag is not an eval.

    /// <summary>
    /// With no <c>replay</c> the session is left alone, whatever the instance's default. The orchestrator sends a run
    /// tag on every fix session, so a default of record would have recorded its real work and one of replay refused it.
    /// </summary>
    [TestCase(WebReplayMode.Off)]
    [TestCase(WebReplayMode.Record)]
    [TestCase(WebReplayMode.Replay)]
    public void ARunTagAlone_IsNotAnEval_AndGetsNoReplay_WhateverTheDefault(WebReplayMode defaultMode)
    {
        Assert.That(Resolve(Request(), defaultMode), Is.EqualTo((true, (SessionReplay?)null, (string?)null)));
        Assert.That(Resolve(Request(), defaultMode, keepsRecordings: false),
            Is.EqualTo((true, (SessionReplay?)null, (string?)null)),
            "and an instance that keeps no recordings has nothing to refuse");
    }

    /// <summary>
    /// A parent that said off passes nothing on, so its specialist isn't an eval either, and doesn't fall back to the
    /// instance's default and record behind its parent's back.
    /// </summary>
    [Test]
    public void AChildSentNothing_GetsNothing_EvenWhereTheDefaultRecords()
    {
        var child = Request(task: "sub-session:puls-cook");

        Assert.That(Resolve(child, WebReplayMode.Record).Replay, Is.Null);
    }

    /// <summary>An eval's <c>replay: {}</c> takes the default for its web: off gives none.</summary>
    [Test]
    public void AnEmptyReplay_TakesTheDefault_OffGivesNone()
    {
        Assert.That(Resolve(Request(replay: Eval)), Is.EqualTo((true, (SessionReplay?)null, (string?)null)));
    }

    /// <summary>With the default record, it records into its run and task.</summary>
    [Test]
    public void AnEmptyReplay_WithTheDefaultRecord_RecordsIntoRunSlashTask()
    {
        var (ok, replay, _) = Resolve(Request(replay: Eval), WebReplayMode.Record);

        Assert.That((ok, replay), Is.EqualTo((true, new SessionReplay(WebReplayMode.Record, $"{Run}/bobotie", false))));
    }

    /// <summary>A recording with no task records under the run tag alone.</summary>
    [Test]
    public void ARecordingWithNoTask_UsesTheRunTagAlone()
    {
        Assert.That(Resolve(Request(task: null, replay: Eval), WebReplayMode.Record).Replay,
            Is.EqualTo(new SessionReplay(WebReplayMode.Record, Run, false)));
        Assert.That(Resolve(Request(task: " ", replay: Eval), WebReplayMode.Record).Replay!.Of, Is.EqualTo(Run));
    }

    /// <summary>With the default replay it has nothing to replay from, so it is refused.</summary>
    [Test]
    public void AnEmptyReplay_WithTheDefaultReplay_NeedsAnOf_SoItIsRefused()
    {
        var (ok, _, refusal) = Resolve(Request(replay: Eval), WebReplayMode.Replay);

        Assert.That(ok, Is.False);
        Assert.That(refusal, Does.Contain("needs 'of'"));
    }

    // Rule 3: web is parsed with SettingChoice.

    /// <summary>Asking for off gives none, even where the default records.</summary>
    [Test]
    public void WebOff_GivesNone_EvenWhereTheDefaultRecords()
    {
        Assert.That(Resolve(Request(replay: new ReplayRequest("off", null)), WebReplayMode.Record),
            Is.EqualTo((true, (SessionReplay?)null, (string?)null)));
    }

    /// <summary>The web's name is read however it is written.</summary>
    [TestCase("record", WebReplayMode.Record)]
    [TestCase("RECORD", WebReplayMode.Record)]
    [TestCase(" Replay ", WebReplayMode.Replay)]
    public void WebIsParsedWithTheCaseIgnored(string web, WebReplayMode expected) =>
        Assert.That(Resolve(Request(replay: new ReplayRequest(web, "run_0:puls-cook:1/bobotie"))).Replay!.Web,
            Is.EqualTo(expected));

    /// <summary>A name we don't know, or a number, is refused with the names that work.</summary>
    [TestCase("sometimes")]
    [TestCase("1")]
    public void AnUnknownWeb_IsRefused_ListingTheSupportedNames(string web)
    {
        var (ok, _, refusal) = Resolve(Request(replay: new ReplayRequest(web, null)));

        Assert.That(ok, Is.False);
        Assert.That(refusal, Does.Contain(web).And.Contain("off, record, replay"));
    }

    /// <summary>No web named takes the instance's.</summary>
    [Test]
    public void AReplayWithNoWeb_UsesTheInstanceDefault()
    {
        Assert.That(
            Resolve(Request(replay: new ReplayRequest(null, "run_0:puls-cook:1/bobotie")), WebReplayMode.Replay).Replay,
            Is.EqualTo(new SessionReplay(WebReplayMode.Replay, "run_0:puls-cook:1/bobotie", false)));
        Assert.That(Resolve(Request(replay: new ReplayRequest(null, null))).Replay, Is.Null);
    }

    // Rule 4: model: true is for web replay only (F18 step 4e).

    /// <summary>Replaying the web can replay the model's answers too.</summary>
    [Test]
    public void ModelTrue_WithWebReplay_GivesAReplayThatAsksForTheModelsAnswers()
    {
        var (ok, replay, _) =
            Resolve(Request(replay: new ReplayRequest("replay", "run_0:puls-cook:1/bobotie", Model: true)));

        Assert.That((ok, replay),
            Is.EqualTo((true, new SessionReplay(WebReplayMode.Replay, "run_0:puls-cook:1/bobotie", Model: true))));
    }

    /// <summary>On an instance whose default is replay, no web named still replays the model.</summary>
    [Test]
    public void ModelTrue_WithNoWeb_OnAnInstanceThatDefaultsToReplay_IsAReplayToo()
    {
        Assert.That(
            Resolve(Request(replay: new ReplayRequest(null, "run_0:puls-cook:1/bobotie", Model: true)),
                WebReplayMode.Replay).Replay!.Model, Is.True);
    }

    /// <summary>
    /// The model's answers can only come from a recording, with its web, so any other web is refused; none named is
    /// the instance's default, off here.
    /// </summary>
    [TestCase("record")]
    [TestCase("off")]
    [TestCase(null)]
    public void ModelTrue_WithAnyOtherWeb_IsRefused(string? web)
    {
        var (ok, replay, refusal) =
            Resolve(Request(replay: new ReplayRequest(web, "run_0:puls-cook:1/bobotie", Model: true)));

        Assert.That((ok, replay), Is.EqualTo((false, (SessionReplay?)null)));
        Assert.That(refusal, Does.Contain("replay.model").And.Contain("'replay'"));
    }

    /// <summary>Replaying the model still needs a trial to read and an instance that keeps recordings.</summary>
    [Test]
    public void ModelTrue_WithWebReplay_StillNeedsOf_AndAnInstanceThatKeepsRecordings()
    {
        Assert.That(Resolve(Request(replay: new ReplayRequest("replay", null, Model: true))).Refusal,
            Does.Contain("needs 'of'"));
        Assert.That(
            Resolve(Request(replay: new ReplayRequest("replay", "x", Model: true)), keepsRecordings: false).Refusal,
            Does.Contain("keeps no recordings"));
    }

    /// <summary>Without <c>model</c> the model runs live.</summary>
    [Test]
    public void ModelFalse_GivesAReplayThatLeavesTheModelAlone() =>
        Assert.That(Resolve(Request(replay: new ReplayRequest("replay", "x"))).Replay!.Model, Is.False);

    // Rule 5: record or replay without REPLAY_DIRECTORY.

    /// <summary>An instance that keeps no recordings refuses to record or replay, the default included.</summary>
    [Test]
    public void RecordOrReplayWithoutAReplayDirectory_IsRefused_ThisInstanceKeepsNoRecordings()
    {
        Assert.That(Resolve(Request(replay: new ReplayRequest("record", null)), keepsRecordings: false).Refusal,
            Does.Contain("keeps no recordings"));
        Assert.That(Resolve(Request(replay: new ReplayRequest("replay", "x")), keepsRecordings: false).Refusal,
            Does.Contain("keeps no recordings"));
        Assert.That(Resolve(Request(replay: Eval), WebReplayMode.Record, keepsRecordings: false).Refusal,
            Does.Contain("keeps no recordings"), "the default is held to it too");
    }

    /// <summary>Off needs no recordings, so it is fine anywhere.</summary>
    [Test]
    public void OffNeedsNoReplayDirectory() =>
        Assert.That(Resolve(Request(replay: new ReplayRequest("off", null)), keepsRecordings: false),
            Is.EqualTo((true, (SessionReplay?)null, (string?)null)));

    // Rule 6: replay without of.

    /// <summary>A replay with no trial to read, or a blank one, is refused.</summary>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void ReplayWithoutOf_IsRefused(string? of)
    {
        var (ok, _, refusal) = Resolve(Request(replay: new ReplayRequest("replay", of)));

        Assert.That(ok, Is.False);
        Assert.That(refusal, Does.Contain("needs 'of'"));
    }

    // Rule 7: record without of is {run}/{task}; with of (a sub-session in its parent's trial) it is kept.

    /// <summary>A recording with no trial named records into its run and task.</summary>
    [Test]
    public void RecordWithoutOf_IsRunSlashTask()
    {
        Assert.That(Resolve(Request(replay: new ReplayRequest("record", null))).Replay,
            Is.EqualTo(new SessionReplay(WebReplayMode.Record, $"{Run}/bobotie", false)));
    }

    /// <summary>
    /// A sub-session keeps the trial it was sent: its own task is <c>sub-session:{name}</c>, and one run tag holds
    /// every scenario of a mode and repeat.
    /// </summary>
    [Test]
    public void ASubSessionsOf_IsKept_ItNeverWorksOutAKeyOfItsOwn()
    {
        var request = Request(task: "sub-session:puls-cook",
            replay: new ReplayRequest("record", "run_1:puls-cook:1/bobotie"));

        Assert.That(Resolve(request).Replay,
            Is.EqualTo(new SessionReplay(WebReplayMode.Record, "run_1:puls-cook:1/bobotie", false)));
    }

    /// <summary>A replay reads the trial it names, as sent.</summary>
    [Test]
    public void AReplayWithOf_IsKeptAsSent() =>
        Assert.That(Resolve(Request(replay: new ReplayRequest("replay", "run_0:puls-cook:1/bobotie"))).Replay,
            Is.EqualTo(new SessionReplay(WebReplayMode.Replay, "run_0:puls-cook:1/bobotie", false)));

    /// <summary><c>replay.of</c> is a tag like the others: as short and as printable.</summary>
    [Test]
    public void ReplayOf_IsATag_ShortAndPrintable()
    {
        Assert.That(Request(replay: new ReplayRequest("replay", new string('x', 200))).InvalidTag(), Is.Null);
        Assert.That(Request(replay: new ReplayRequest("replay", new string('x', 201))).InvalidTag(),
            Is.EqualTo("replay.of"));
        Assert.That(Request(replay: new ReplayRequest("replay", "a\nb")).InvalidTag(), Is.EqualTo("replay.of"));
        Assert.That(Request().InvalidTag(), Is.Null);
    }
}
