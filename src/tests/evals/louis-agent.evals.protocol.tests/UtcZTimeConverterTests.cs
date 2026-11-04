using LouisAgent.Evals.Protocol.Events.Run;

namespace LouisAgent.Evals.Protocol.Tests;

/// <summary>
/// A line's time is written in UTC ending in <c>Z</c>, as the dates rule writes it, and read back whether it ends in
/// <c>Z</c> or carries an offset.
/// </summary>
public class UtcZTimeConverterTests
{
    /// <summary>The line of a small event written at the time given.</summary>
    private static string LineAt(DateTimeOffset t) => EvalProtocolJson.Line(new ModeDoneEvent(t, "m", 1, null));

    /// <summary>A UTC time ends in Z, with no offset written.</summary>
    [Test]
    public void AUtcTime_IsWrittenWithZ_AndNoOffset()
    {
        string line = LineAt(new DateTimeOffset(2026, 10, 28, 8, 0, 0, TimeSpan.Zero));

        Assert.That(line, Does.Contain("\"t\":\"2026-10-28T08:00:00Z\""));
        Assert.That(line, Does.Not.Contain("+00:00"));
    }

    /// <summary>A fraction of a second is kept, without the zeros that trail it.</summary>
    [Test]
    public void AFractionOfASecond_IsKept()
    {
        string line = LineAt(new DateTimeOffset(2026, 10, 28, 9, 30, 15, 250, TimeSpan.Zero));

        Assert.That(line, Does.Contain("\"t\":\"2026-10-28T09:30:15.25Z\""));
    }

    /// <summary>A time with an offset is written as the same moment in UTC.</summary>
    [Test]
    public void ATimeWithAnOffset_IsWrittenAsItsUtcTime()
    {
        string line = LineAt(new DateTimeOffset(2026, 10, 28, 10, 0, 0, TimeSpan.FromHours(2)));

        Assert.That(line, Does.Contain("\"t\":\"2026-10-28T08:00:00Z\""));
    }

    /// <summary>A Z, a zero offset and a two-hour offset for the same moment all read as that moment.</summary>
    [TestCase("2026-10-28T08:00:00Z")]
    [TestCase("2026-10-28T08:00:00+00:00")]
    [TestCase("2026-10-28T10:00:00+02:00")]
    public void ATimeIsRead_WhetherItEndsInZOrAnOffset(string t)
    {
        string line = $$"""{"event":"mode-done","t":"{{t}}","mode":"m","repeat":1,"rates":null}""";

        var read = EvalProtocolJson.Read(line);

        Assert.That(read?.T, Is.EqualTo(new DateTimeOffset(2026, 10, 28, 8, 0, 0, TimeSpan.Zero)));
    }
}
