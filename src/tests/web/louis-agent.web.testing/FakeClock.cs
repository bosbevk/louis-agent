namespace LouisAgent.Web.Testing;

/// <summary>A clock a test holds still or moves by hand, for what throttles on time (ChatRunner's redraw).</summary>
public sealed class FakeClock(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
