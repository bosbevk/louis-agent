using LouisAgent.Evals.Runner.Arguments;

namespace LouisAgent.Evals.Runner.Tests.Arguments;

// The run's name (F18 step 4.1 review, and 4.2 review): unset, the run is local-{yyyyMMdd-HHmmss} from the clock, as the Python checks
// name it; set, it is the variable, trimmed. Written first: the reader threw NotImplementedException on the red run.
public class BenchmarkRunTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void UnsetIsLocalWithTheTimeOfTheRun(string? value) =>
        Assert.That(BenchmarkRun.Of(value, new FixedClock(At)), Is.EqualTo("local-20261025-140000"));

    [Test]
    public void TheVariableIsTheRunTrimmed() =>
        Assert.That(BenchmarkRun.Of(" portal-run-7 ", new FixedClock(At)), Is.EqualTo("portal-run-7"));

    /// <summary>A clock at one instant, whose local time is two hours on: a fixed zone, so the test is the same on every machine.</summary>
    private sealed class FixedClock(DateTimeOffset at) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => at;

        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.CreateCustomTimeZone("fixed", TimeSpan.FromHours(2), "fixed", "fixed");
    }
}
