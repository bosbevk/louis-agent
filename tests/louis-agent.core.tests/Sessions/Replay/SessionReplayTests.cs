using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Core.Tests.Sessions.Replay;

// F18 step 4a.1: the session's replay settings are held in an AsyncLocal as UserPlace is, and WEB_REPLAY is parsed once where it enters
// (ADR 0024). Written first: every member threw NotImplementedException on the red run.
public class SessionReplayTests
{
    [Test]
    public void Use_RestoresThePreviousValue()
    {
        Assert.That(SessionReplay.Current, Is.Null);
        var outer = new SessionReplay(WebReplayMode.Record, "run_1:a:1/bobotie", false);
        var inner = new SessionReplay(WebReplayMode.Replay, "run_0:a:1/bobotie", false);

        using (SessionReplay.Use(outer))
        {
            using (SessionReplay.Use(inner)) Assert.That(SessionReplay.Current, Is.SameAs(inner));
            Assert.That(SessionReplay.Current, Is.SameAs(outer));
        }

        Assert.That(SessionReplay.Current, Is.Null);
    }

    [TestCase(null, WebReplayMode.Off)]
    [TestCase("", WebReplayMode.Off)]
    [TestCase("record", WebReplayMode.Record)]
    [TestCase("REPLAY", WebReplayMode.Replay)]
    [TestCase(" off ", WebReplayMode.Off)]
    public void DefaultMode_ParsesTheSettingWithTheCaseIgnored(string? setting, WebReplayMode expected) =>
        Assert.That(SessionReplay.DefaultMode(setting), Is.EqualTo(expected));

    [Test]
    public void DefaultMode_RefusesAnUnknownValue_NamingItAndTheSupportedOnes()
    {
        var error = Assert.Throws<ArgumentException>(() => SessionReplay.DefaultMode("sometimes"))!;

        Assert.That(error.Message, Does.Contain("sometimes").And.Contain("WEB_REPLAY").And.Contain("off").And.Contain("record").And.Contain("replay"));
    }

    [Test]
    public void DefaultMode_NeverTakesANumber() =>
        Assert.Throws<ArgumentException>(() => SessionReplay.DefaultMode("1"));
}
