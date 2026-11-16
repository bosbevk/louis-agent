using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Site.Components.Architecture.Framework;

/// <summary>The eval harness on the Architecture page: the four projects every check runs on.</summary>
public partial class EvalsFamily
{
    /// <summary>Each project of the harness, and what it is.</summary>
    private static readonly IReadOnlyList<Term> Terms =
    [
        new(["louis-agent.evals"], "The harness: datasets, trials, the common graders, the model judge and its calibration against human labels, the runner loop."),
        new(["louis-agent.evals.checks"], "The check contract, out of the harness so a pack can ship its checks without it."),
        new(["louis-agent.evals.protocol"], "The events a run writes and the API and the pages read: records and enums only."),
        new(["louis-agent.evals.runner"], "The command that lists and runs evals, re-scores a run and checks a judge. A demo's program starts it, and the API starts it as a child process.")
    ];
}
