using LouisAgent.Api.Evals.List;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Tests.Evals.List;

/// <summary>
/// What the portal can run: every eval the runner lists, read once, the first time somebody asks.
/// </summary>
public class EvalListTests
{
    /// <summary>A catalogue given nothing offers nothing.</summary>
    [Test]
    public void AnEmptyCatalog_HasNoChecks() =>
        Assert.That(new EvalList().Checks, Is.Empty);

    /// <summary>A catalogue a test fills offers exactly those checks.</summary>
    [Test]
    public void ACatalog_OffersTheChecksItIsGiven()
    {
        var check = new EvalEntry(SampleEval.Beta, "Die kokkontrole");

        Assert.That(new EvalList([check]).Checks, Is.EqualTo(new[] { check }));
    }

    /// <summary>A check keeps the manual path it was given.</summary>
    [Test]
    public void AnEvalCheckHasTheManualItsEvalListed() =>
        Assert.That(new EvalEntry("stub", "Stub", "evals/louis-agent.evals.runner/Manuals/stub.md").ManualPath,
            Is.EqualTo("evals/louis-agent.evals.runner/Manuals/stub.md"));

    /// <summary>The manual's path is the one the runner listed, or none.</summary>
    [Test]
    public void TheCatalogue_KeepsTheManualPathTheRunnerListed()
    {
        var merged = EvalList.Merge([
            new EvalLine(SampleEval.Beta, 9, "A meal", SampleIdentity.Alpha,
                "identities/louis-agent.identity.puls/Evals/Cook/manual.md"),
            new EvalLine("quiet", 1, "No manual")
        ]);

        Assert.That(merged.Select(c => c.ManualPath),
            Is.EqualTo(new string?[] { "identities/louis-agent.identity.puls/Evals/Cook/manual.md", null }));
    }

    /// <summary>Each listed eval is a check under its name, titled with what it proves.</summary>
    [Test]
    public void EachListedEval_IsACheck_WithItsProvesAsItsTitle()
    {
        var merged = EvalList.Merge([
            new EvalLine("stub", 1, "A canned run", SampleIdentity.Alpha),
            new EvalLine(SampleEval.Beta, 9, "A meal", SampleIdentity.Alpha)
        ]);

        Assert.That(merged.Select(c => (c.Id, c.Title)),
            Is.EqualTo(new[] { ("stub", "A canned run"), (SampleEval.Beta, "A meal") }));
    }

    /// <summary>
    /// A check keeps the whole line the runner listed, so the eval list can show its graders and dataset without
    /// asking the runner again.
    /// </summary>
    [Test]
    public void EachCheck_KeepsTheLineItsEvalListed()
    {
        var line = new EvalLine("stub", 1, "A canned run", SampleIdentity.Alpha);

        var merged = EvalList.Merge([line]);

        Assert.That(merged.Single().Eval, Is.SameAs(line));
    }

    /// <summary>With nothing listed there is nothing to run.</summary>
    [Test]
    public void WithNoListedEvals_ThereAreNoChecks() =>
        Assert.That(EvalList.Merge([]), Is.Empty);

    /// <summary>A listing that fails leaves no checks, and the log says why.</summary>
    [Test]
    public void AListingThatFails_LeavesNoChecks_AndSaysWhy()
    {
        var log = new CapturingLogger();

        var catalog = EvalList.From(EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll"), log);

        Assert.That(catalog.Checks, Is.Empty, "no Python to fall back on (F18 step 7.4)");
        Assert.That(log.Messages, Has.Some.Contains("isn't at /nope/louis-agent.evals.runner.dll"));
    }

    /// <summary>Nothing runs until somebody asks for the checks.</summary>
    [Test]
    public void TheListingIsReadOnFirstUse()
    {
        var log = new CapturingLogger();

        var catalog = EvalList.From(EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll"), log);
        Assert.That(log.Messages, Is.Empty, "nothing runs until the checks are asked for");

        // Asking for the list is what reads it.
        _ = catalog.Checks;
        Assert.That(log.Messages, Is.Not.Empty);
    }

    /// <summary>Warm reads the listing straight away, so start-up's log carries the warning.</summary>
    [Test]
    public void WarmReadsTheListingNow()
    {
        var log = new CapturingLogger();
        var catalog = EvalList.From(EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll"), log);

        catalog.Warm();

        Assert.That(log.Messages, Has.Some.Contains("isn't at /nope/louis-agent.evals.runner.dll"),
            "start-up says why, not the first request");
    }

    /// <summary>Keeps what the catalogue logs, formatted, so a test can read the warning.</summary>
    private sealed class CapturingLogger : ILogger
    {
        /// <summary>The lines logged so far, in order.</summary>
        public List<string> Messages { get; } = [];

        /// <summary>No scopes: the test only reads the lines.</summary>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <summary>Every level is kept.</summary>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <summary>Keeps the line as it would be written.</summary>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
