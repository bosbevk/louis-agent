using LouisAgent.Testing;

using LouisAgent.Api.Benchmarks.Runs;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F27 step 1 (F27-S3): the checks the portal can run. Since F18 step 7.4 every check is an eval the runner lists: no script, no Python fallback.
public class BenchmarkCatalogTests
{
    [Test]
    public void AnEmptyCatalog_HasNoChecks() =>
        Assert.That(new BenchmarkCatalog().Checks, Is.Empty);

    [Test]
    public void ACatalog_OffersTheChecksItIsGiven()
    {
        var check = new BenchmarkCheck(SampleEval.Beta, "Die kokkontrole");

        Assert.That(new BenchmarkCatalog([check]).Checks, Is.EqualTo(new[] { check }));
    }

    [Test]
    public void AnEvalCheckHasTheManualItsEvalListed() =>
        Assert.That(new BenchmarkCheck("stub", "Stub", "evals/louis-agent.evals.runner/Manuals/stub.md").ManualPath, Is.EqualTo("evals/louis-agent.evals.runner/Manuals/stub.md"));

    [Test]
    public void TheCatalogue_KeepsTheManualPathTheRunnerListed()
    {
        var merged = BenchmarkCatalog.Merge([new EvalLine(SampleEval.Beta, 9, "A meal", SampleIdentity.Alpha, "evals/louis-agent.evals.puls/Manuals/cook.md"), new EvalLine("quiet", 1, "No manual")]);

        Assert.That(merged.Select(c => c.ManualPath), Is.EqualTo(new string?[] { "evals/louis-agent.evals.puls/Manuals/cook.md", null }));
    }

    [Test]
    public void EachListedEval_IsACheck_WithItsProvesAsItsTitle()
    {
        var merged = BenchmarkCatalog.Merge([new EvalLine("stub", 1, "A canned run", SampleIdentity.Alpha), new EvalLine(SampleEval.Beta, 9, "A meal", SampleIdentity.Alpha)]);

        Assert.That(merged.Select(c => (c.Id, c.Title)), Is.EqualTo(new[] { ("stub", "A canned run"), (SampleEval.Beta, "A meal") }));
    }

    [Test]
    public void WithNoListedEvals_ThereAreNoChecks() =>
        Assert.That(BenchmarkCatalog.Merge([]), Is.Empty);

    [Test]
    public void AListingThatFails_LeavesNoChecks_AndSaysWhy()
    {
        var log = new CapturingLogger();

        var catalog = BenchmarkCatalog.From(EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll"), log);

        Assert.That(catalog.Checks, Is.Empty, "no Python to fall back on (F18 step 7.4)");
        Assert.That(log.Messages, Has.Some.Contains("isn't at /nope/louis-agent.evals.runner.dll"));
    }

    [Test]
    public void TheListingIsReadOnFirstUse()
    {
        var log = new CapturingLogger();

        var catalog = BenchmarkCatalog.From(EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll"), log);
        Assert.That(log.Messages, Is.Empty, "nothing runs until the checks are asked for");

        _ = catalog.Checks;
        Assert.That(log.Messages, Is.Not.Empty);
    }

    [Test]
    public void WarmReadsTheListingNow()
    {
        var log = new CapturingLogger();
        var catalog = BenchmarkCatalog.From(EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll"), log);

        catalog.Warm();

        Assert.That(log.Messages, Has.Some.Contains("isn't at /nope/louis-agent.evals.runner.dll"), "start-up says why, not the first request");
    }

    /// <summary>Keeps what the catalogue logs, formatted, so a test can read the warning.</summary>
    private sealed class CapturingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
