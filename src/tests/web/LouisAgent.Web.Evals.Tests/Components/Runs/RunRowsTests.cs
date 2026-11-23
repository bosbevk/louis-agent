using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Components.Runs;

namespace LouisAgent.Web.Evals.Tests.Components.Runs;

/// <summary>An eval's runs at the foot of a run's page: each a link with what it ran and how it went.</summary>
public class RunRowsTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 6, 19, 0, TimeSpan.Zero);

    /// <summary>Each run is a link to its page, with its kind, its repeats and passed of conversations.</summary>
    [Test]
    public void EachRun_IsALink_WithItsKindRepeatsAndPassed()
    {
        using var ctx = new BunitContext();
        var row = new RunRow("20261123-061900-guide", RunKind.Replay, RunStatus.Finished, 3, Started,
            Started.AddSeconds(30), 5, 7);

        var rows = ctx.Render<RunRows>(parameters => parameters.Add(p => p.Eval, "guide").Add(p => p.Rows, [row])
            .Add(p => p.Open, "another-run"));

        var link = rows.Find("a.run-row");
        Assert.That(link.GetAttribute("href"), Is.EqualTo(EvalAddress.RunPageOf("guide", row.Id)));
        Assert.That(link.TextContent, Does.Contain("replay").And.Contain("3 repeats").And.Contain("5 of 7 passed"));
    }

    /// <summary>The open run is marked for the eye and for a screen reader.</summary>
    [Test]
    public void TheOpenRun_IsMarked()
    {
        using var ctx = new BunitContext();
        var row = new RunRow("20261123-061900-guide", RunKind.Live, RunStatus.Finished, 1, Started, null, 1, 1);

        var rows = ctx.Render<RunRows>(parameters => parameters.Add(p => p.Eval, "guide").Add(p => p.Rows, [row])
            .Add(p => p.Open, row.Id));

        Assert.That(rows.Find("a.run-row").GetAttribute("aria-current"), Is.EqualTo("page"));
    }
}
