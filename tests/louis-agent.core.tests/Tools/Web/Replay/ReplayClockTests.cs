using System.Text.Json;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Tools.Clock;
using LouisAgent.Core.Tools.Web.Replay;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Core.Tests.Tools.Web.Replay;

// F18 step 4b.4: the clock, recorded and replayed. Within a trial it stands still: the first read is saved and every later read answers
// it, so a replay shows the model the same time as the recording, and step 4e's cache of model answers keeps hitting. Written first:
// ReplayClock.GetUtcNow threw NotImplementedException on the red run.
public class ReplayClockTests
{
    private const string Johannesburg = "Africa/Johannesburg";

    private static IDisposable Using(WebReplayMode mode, string? of) =>
        SessionReplay.Use(new SessionReplay(mode, of, Model: false));

    private static string Today(TimeProvider clock) => new ClockTools(clock, Johannesburg).GetToday();

    [Test]
    public void RecordingThenReplaying_AfterTheRealTimeMovedOn_GivesTheSameToolText()
    {
        using var folder = new ReplayFolder();
        var inner = new FakeTimeProvider(new DateTimeOffset(2026, 10, 25, 6, 0, 0, TimeSpan.Zero));

        string recorded, replayed;
        using (Using(WebReplayMode.Record, "t")) recorded = Today(new ReplayClock(folder.Store, inner));
        inner.Advance(TimeSpan.FromHours(3));
        using (Using(WebReplayMode.Replay, "t")) replayed = Today(new ReplayClock(folder.Store, inner));

        Assert.That(recorded, Does.StartWith("Sunday 25 October 2026, 08:00 in Africa/Johannesburg"));
        Assert.That(replayed, Is.EqualTo(recorded));
    }

    [Test]
    public void WhileRecording_TheClockStandsStill_AndTheTrialHasOneIndexLine()
    {
        using var folder = new ReplayFolder();
        var inner = new FakeTimeProvider(new DateTimeOffset(2026, 10, 25, 6, 0, 0, TimeSpan.Zero));
        var clock = new ReplayClock(folder.Store, inner);

        DateTimeOffset first, second;
        using (Using(WebReplayMode.Record, "t"))
        {
            first = clock.GetUtcNow();
            inner.Advance(TimeSpan.FromMinutes(7));
            second = clock.GetUtcNow();
        }

        Assert.That(second, Is.EqualTo(first));
        var line = JsonDocument.Parse(File.ReadAllLines(folder.Trials.Single()).Single()).RootElement;
        Assert.That(line.GetProperty("kind").GetString(), Is.EqualTo("clock"));
        Assert.That(line.GetProperty("key").GetString(), Is.EqualTo("{}"));
    }

    [Test]
    public void WithNoTrial_TheClockFollowsTheInnerOne_AndTheFolderStaysEmpty()
    {
        using var folder = new ReplayFolder();
        var inner = new FakeTimeProvider(new DateTimeOffset(2026, 10, 25, 6, 0, 0, TimeSpan.Zero));
        var clock = new ReplayClock(folder.Store, inner);

        var before = clock.GetUtcNow();
        inner.Advance(TimeSpan.FromHours(1));
        DateTimeOffset after, off;
        using (Using(WebReplayMode.Off, null)) off = clock.GetUtcNow();
        after = clock.GetUtcNow();

        Assert.That(after - before, Is.EqualTo(TimeSpan.FromHours(1)));
        Assert.That(off, Is.EqualTo(after));
        Assert.That(folder.Entries, Is.Empty);
    }

    [Test]
    public void AReplayOfATrialThatNeverReadTheClock_ThrowsTheMissText_ForTheRestOfTheTrial()
    {
        using var folder = new ReplayFolder();
        var clock = new ReplayClock(folder.Store, new FakeTimeProvider());

        using (Using(WebReplayMode.Replay, "t"))
        {
            var first = Assert.Throws<InvalidOperationException>(() => clock.GetUtcNow())!;
            Assert.That(first.Message,
                Does.Contain("never read the clock").And.Contain("treat today's date and the time as unknown"));
            Assert.Throws<InvalidOperationException>(() => clock.GetUtcNow(),
                "a Lazy keeps its exception: a miss stays a miss for the trial");
        }

        Assert.That(folder.Entries, Is.Empty);
    }

    [Test]
    public void TwoTrialsRecordedAtDifferentTimes_KeepTheirOwnTimes()
    {
        using var folder = new ReplayFolder();
        var inner = new FakeTimeProvider(new DateTimeOffset(2026, 10, 25, 6, 0, 0, TimeSpan.Zero));
        var clock = new ReplayClock(folder.Store, inner);
        DateTimeOffset a, b;
        using (Using(WebReplayMode.Record, "a")) a = clock.GetUtcNow();
        inner.Advance(TimeSpan.FromHours(5));
        using (Using(WebReplayMode.Record, "b")) b = clock.GetUtcNow();

        var replay = new ReplayClock(folder.Store, new FakeTimeProvider());
        using (Using(WebReplayMode.Replay, "a")) Assert.That(replay.GetUtcNow(), Is.EqualTo(a));
        using (Using(WebReplayMode.Replay, "b")) Assert.That(replay.GetUtcNow(), Is.EqualTo(b));
        Assert.That(b - a, Is.EqualTo(TimeSpan.FromHours(5)));
    }

    [Test]
    public void TwentyParallelReadsInOneRecordingTrial_SaveOneIndexLine()
    {
        // A parent and its sub-session read at the same moment: the Lazy makes it one recording, not two.
        using var folder = new ReplayFolder();
        var clock = new ReplayClock(folder.Store,
            new FakeTimeProvider(new DateTimeOffset(2026, 10, 25, 6, 0, 0, TimeSpan.Zero)));

        DateTimeOffset[] times;
        using (Using(WebReplayMode.Record, "t"))
            times = Enumerable.Range(0, 20).AsParallel().WithDegreeOfParallelism(20).Select(_ => clock.GetUtcNow())
                .ToArray();

        Assert.That(times.Distinct().Count(), Is.EqualTo(1));
        Assert.That(File.ReadAllLines(folder.Trials.Single()), Has.Length.EqualTo(1));
    }
}
