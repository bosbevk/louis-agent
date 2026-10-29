using LouisAgent.Puls.Admin.Notes;

namespace LouisAgent.Puls.Tests.Admin.Notes;

/// <summary>The words a notes panel shows.</summary>
public class NoteTextTests
{
    /// <summary>A note's time is shown in the reader's own time, to the minute.</summary>
    [Test]
    public void When_ShowsTheLocalTime_ToTheMinute()
    {
        // A time already at this machine's own offset, so the conversion to local time leaves it as written.
        var local = new DateTimeOffset(2026, 10, 27, 14, 30, 45,
            TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 10, 27, 14, 30, 45)));

        Assert.That(NoteText.When(local), Is.EqualTo("2026-10-27 14:30"));
    }
}
