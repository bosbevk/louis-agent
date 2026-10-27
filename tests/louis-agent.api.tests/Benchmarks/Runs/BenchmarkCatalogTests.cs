
using LouisAgent.Api.Benchmarks.Runs;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F27 step 1 (F27-S3): the checks the portal can run; F31 brings them back, each with its script under benchmarks/.
public class BenchmarkCatalogTests
{
    private static readonly BenchmarkCheck[] ScriptChecks = [new BenchmarkCheck("cook", "Kook", "scripts/cook/run_check.py")];

    [Test]
    public void AnEmptyCatalog_HasNoChecks() =>
        Assert.That(new BenchmarkCatalog().Checks, Is.Empty);

    [Test]
    public void ThePortalHasNoPythonChecks_EveryCheckIsAnEvalTheRunnerLists()
    {
        // F18 step 7.3: benchmarks/ is deleted; the stub, the cook and the routing are the runner's evals (stub, puls-cook, puls-routing).
        Assert.That(BenchmarkCatalog.Default, Is.Empty);
    }

    [Test]
    public void ACatalog_OffersTheChecksItIsGiven()
    {
        var check = new BenchmarkCheck("cook", "Die kokkontrole", "benchmarks/cook/run_check.py", ["--x"]);

        Assert.That(new BenchmarkCatalog([check]).Checks, Is.EqualTo(new[] { check }));
    }

    [Test]
    public void AnEvalCheckHasTheManualItsEvalListed() =>
        Assert.That(BenchmarkCheck.ForEval("stub", "Stub", "evals/louis-agent.evals.runner/Manuals/stub.md").ManualPath, Is.EqualTo("evals/louis-agent.evals.runner/Manuals/stub.md"));

    [Test]
    public void TheCatalogue_KeepsTheManualPathTheRunnerListed()
    {
        var merged = BenchmarkCatalog.Merge([new EvalLine("puls-cook", 9, "A meal", "puls", "evals/louis-agent.evals.puls/Manuals/cook.md"), new EvalLine("quiet", 1, "No manual")], []);

        Assert.That(merged.Select(c => c.ManualPath), Is.EqualTo(new string?[] { "evals/louis-agent.evals.puls/Manuals/cook.md", null }));
    }

    [Test]
    public void AListedEvalHidesThePythonCheckWithItsId()
    {
        var python = new[] { new BenchmarkCheck("stub", "Stub", "scripts/stub/run_check.py"), new BenchmarkCheck("cook", "Kook", "scripts/cook/run_check.py"), new BenchmarkCheck("routing", "Roetes", "scripts/routing/run_check.py") };
        var merged = BenchmarkCatalog.Merge([new EvalLine("stub", 1, "A canned run", "puls")], python);

        Assert.That(merged.Select(c => c.Id), Is.EqualTo(new[] { "stub", "cook", "routing" }));
        Assert.That((merged[0].Kind, merged[0].Title), Is.EqualTo((BenchmarkCheckKind.Eval, "A canned run")));
        Assert.That(merged.Skip(1).All(c => c.Kind == BenchmarkCheckKind.Script), Is.True);
    }

    [Test]
    public void WithNoListedEvals_TheScriptChecksGivenStay() =>
        Assert.That(BenchmarkCatalog.Merge([], [new BenchmarkCheck("cook", "Kook", "scripts/cook/run_check.py")]).Select(c => c.Id), Is.EqualTo(new[] { "cook" }));

    [Test]
    public void AListingThatFails_KeepsTheScriptChecksGivenAndSaysWhy()
    {
        var log = new CapturingLogger();

        var catalog = BenchmarkCatalog.From(EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll"), ScriptChecks, log);

        Assert.That(catalog.Checks.Select(c => c.Id), Is.EqualTo(new[] { "cook" }));
        Assert.That(log.Messages, Has.Some.Contains("isn't at /nope/louis-agent.evals.runner.dll"));
    }

    [Test]
    public void TheListingIsReadOnFirstUse()
    {
        var log = new CapturingLogger();

        var catalog = BenchmarkCatalog.From(EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll"), BenchmarkCatalog.Default, log);
        Assert.That(log.Messages, Is.Empty, "nothing runs until the checks are asked for");

        _ = catalog.Checks;
        Assert.That(log.Messages, Is.Not.Empty);
    }

    [Test]
    public void WarmReadsTheListingNow()
    {
        var log = new CapturingLogger();
        var catalog = BenchmarkCatalog.From(EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll"), BenchmarkCatalog.Default, log);

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
