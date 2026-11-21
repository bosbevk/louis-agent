using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Core.Tests.Sessions.Replay;

/// <summary>
/// The session's replay settings are held in an <c>AsyncLocal</c> as <c>UserPlace</c> is, and <c>WEB_REPLAY</c> is
/// parsed once where it enters (F18 step 4a.1, ADR 0024). Written first: every member threw
/// <c>NotImplementedException</c> on the red run.
/// </summary>
public class SessionReplayTests
{
    /// <summary>Settings used inside others give the outer ones back when they end.</summary>
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

    /// <summary>
    /// A sub-session is sent this flow's settings as the API reads them, and nothing when there are none.
    /// </summary>
    [Test]
    public void ForSubSession_IsThisFlowsSettingsAsTheApiReadsThem_AndNullWithNone()
    {
        Assert.That(SessionReplay.ForSubSession(), Is.Null);

        using (SessionReplay.Use(new SessionReplay(WebReplayMode.Record, "run_1:a:1/bobotie", false)))
            Assert.That(System.Text.Json.JsonSerializer.Serialize(SessionReplay.ForSubSession(),
                    System.Text.Json.JsonSerializerOptions.Web),
                Is.EqualTo("""{"web":"record","of":"run_1:a:1/bobotie","model":false}"""));
    }

    /// <summary>The setting is read however it is written; unset or blank is off.</summary>
    [TestCase(null, WebReplayMode.Off)]
    [TestCase("", WebReplayMode.Off)]
    [TestCase("record", WebReplayMode.Record)]
    [TestCase("REPLAY", WebReplayMode.Replay)]
    [TestCase(" off ", WebReplayMode.Off)]
    public void DefaultMode_ParsesTheSettingWithTheCaseIgnored(string? setting, WebReplayMode expected) =>
        Assert.That(SessionReplay.DefaultMode(setting), Is.EqualTo(expected));

    /// <summary>A value we don't know stops start-up, naming it and the ones that work.</summary>
    [Test]
    public void DefaultMode_RefusesAnUnknownValue_NamingItAndTheSupportedOnes()
    {
        var error = Assert.Throws<ArgumentException>(() => SessionReplay.DefaultMode("sometimes"))!;

        Assert.That(error.Message,
            Does.Contain("sometimes").And.Contain("WEB_REPLAY").And.Contain("off").And.Contain("record").And
                .Contain("replay"));
    }

    /// <summary>A number is no mode, even one that would be the enum's value.</summary>
    [Test]
    public void DefaultMode_NeverTakesANumber() =>
        Assert.Throws<ArgumentException>(() => SessionReplay.DefaultMode("1"));
}
