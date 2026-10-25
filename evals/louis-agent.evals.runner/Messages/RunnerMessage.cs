using System.ComponentModel;

namespace LouisAgent.Evals.Runner.Messages;

/// <summary>
/// The runner's refusals, usage and grader reasons (ADR 0009: a text is a message enum member). Refusals go to stderr; stdout
/// carries only the protocol and the listing.
/// </summary>
public enum RunnerMessage
{
    [Description("Unknown EVALS value '{Name}'. The sets are {Supported}.")]
    UnknownEvalSet,

    [Description("No eval is named '{Name}'. The evals are {Supported}.")]
    UnknownEval,

    [Description("Eval '{Name}' has a scenario, '{Scenario}', with no messages, so there is nothing to run.")]
    NoMessages,

    [Description("Two evals are named '{Name}'.")]
    DuplicateEval,

    [Description("Eval '{Name}' has no graders, so it can't run.")]
    NoGraders,

    [Description("Eval '{Name}' has no required fixed-rule grader, so it can't run without a check that needs no model.")]
    NoFixedRuleGrader,

    [Description("--repeat needs a whole number, not '{Value}'.")]
    BadRepeat,

    [Description("Usage: run <eval> [--repeat N] [--web off|record|replay] [--of <runId>] [--replay <runId>], or --list.")]
    Usage,

    [Description("Unknown argument '{Argument}'. Usage: run <eval> [--repeat N] [--web off|record|replay] [--of <runId>] [--replay <runId>], or --list.")]
    UnknownArgument,

    [Description("Unknown --web '{Value}'. Use one of: {Supported}.")]
    UnknownWeb,

    [Description("--web replay needs --of: the recorded run whose pages it replays.")]
    ReplayNeedsOf,

    [Description("--of needs --web replay: it names the recorded run to replay, and only a replay reads one.")]
    OfNeedsReplay,

    [Description("--replay <runId> is --web replay --of <runId> with the model's answers too, so it can't be given with --web or --of.")]
    ReplayExcludesWebAndOf,

    [Description("--of takes a run id's letters, digits, '-' and '_', not '{Value}'.")]
    BadOf,

    [Description("Eval '{Name}' needs an HTTP session, which step 5 brings; only the stub runs for now.")]
    NoSessionClient,

    [Description("The stub's canned run: it proves the portal, not Puls, and costs nothing.")]
    StubProves,
}
