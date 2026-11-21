namespace LouisAgent.Evals.Tests;

/// <summary>A clock that always says the same instant, so the protocol's times and the durations are fixed.</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
