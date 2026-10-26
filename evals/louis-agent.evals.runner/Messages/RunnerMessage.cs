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

    [Description("Usage: run <eval> [--repeat N] [--web off|record|replay] [--of <runId>] [--replay <runId>] [--split dev|held-out|all], run <eval> --rescore <runId>, --list, --calibrate <eval> <grader>, or --label-sheet <eval> <grader> <runId> <folder> [<count>].")]
    Usage,

    [Description("Unknown argument '{Argument}'. Usage: run <eval> [--repeat N] [--web off|record|replay] [--of <runId>] [--replay <runId>] [--split dev|held-out|all], run <eval> --rescore <runId>, --list, --calibrate <eval> <grader>, or --label-sheet <eval> <grader> <runId> <folder> [<count>].")]
    UnknownArgument,

    [Description("--calibrate asks a model, so it is never run in CI. Run it by hand, with the judge you mean to measure (EVAL_JUDGE_*).")]
    CalibrationNotInCi,

    [Description("Eval '{Eval}' has no grader '{Grader}'. Its graders are {Graders}.")]
    NoSuchGrader,

    [Description("Grader '{Grader}' of eval '{Eval}' is a fixed rule, not a model. Only a model grader is calibrated against a person.")]
    GraderNotModel,

    [Description("Eval '{Eval}' has no labels for the grader '{Grader}'. Make a sheet from a stored run with --label-sheet {Eval} <grader> <runId> <folder>, label it, and put the files in its dataset's labels/ folder.")]
    NoLabels,

    [Description("{Count} label file(s) written to {Folder}. Give each a label (\"pass\" true or false, and a note) and copy them to the eval's dataset in labels/; a file with no label stops the eval from loading.")]
    LabelSheetWritten,

    [Description("--label-sheet's count needs a whole number of at least 1, not '{Value}'.")]
    BadLabelCount,

    [Description("Unknown --web '{Value}'. Use one of: {Supported}.")]
    UnknownWeb,

    [Description("Unknown --split '{Value}'. Use one of: {Supported} (held-out may be written with its dash).")]
    UnknownSplit,

    [Description("--web replay needs --of: the recorded run whose pages it replays.")]
    ReplayNeedsOf,

    [Description("--of needs --web replay: it names the recorded run to replay, and only a replay reads one.")]
    OfNeedsReplay,

    [Description("--replay <runId> is --web replay --of <runId> with the model's answers too, so it can't be given with --web or --of.")]
    ReplayExcludesWebAndOf,

    [Description("--of takes a run id's letters, digits, '-' and '_', not '{Value}'.")]
    BadOf,

    [Description("--rescore <runId> re-grades a stored run's transcripts and opens no session, so it can't be given with --web, --replay, --repeat or --split (the repeats come from the stored run).")]
    RescoreExcludesRunFlags,

    [Description("The stub's canned run: it proves the portal, not Puls, and costs nothing.")]
    StubProves,
}
