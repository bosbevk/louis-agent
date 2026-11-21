using LouisAgent.Composition.Messages;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Api.Evals.Messages;

namespace LouisAgent.Api.Evals.Studio;

/// <summary>
/// What the studio asks the eval runner for: its catalogue of checks and readers, and its judgement of a folder. One
/// runner path makes both; a host with no runner gets parts that say so.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler): the admin routes take the studio's runner-backed parts as one value, so a step
/// that adds a runner command (try, publish) adds a member here, not another parameter to every host's mapping.
/// </remarks>
public sealed record StudioRunner(CheckList Checks, DraftChecker Checker, PromptfooRunner Promptfoo)
{
    /// <summary>The parts over the runner at this path.</summary>
    public static StudioRunner ForRunner(string runnerPath) =>
        new(new CheckList(CheckListing.ForRunner(runnerPath).Read), DraftChecker.ForRunner(runnerPath),
            PromptfooRunner.ForRunner(runnerPath));

    /// <summary>Parts for a host with no runner: each says the runner isn't there.</summary>
    public static StudioRunner None { get; } = new(
        new CheckList(() => new CheckListingResult([], [], NoRunner)),
        new DraftChecker(_ => new RunnerOutput("", NoRunner)),
        new PromptfooRunner(_ => new RunnerOutput("", NoRunner)));

    /// <summary>What the parts say when there is no runner.</summary>
    private static string NoRunner => MessageText.Format(EvalListingMessage.Missing, new { Path = "(none)" });
}
