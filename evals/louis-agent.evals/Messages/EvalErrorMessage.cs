using System.ComponentModel;

namespace LouisAgent.Evals.Messages;

/// <summary>
/// The texts of the eval's refusals, such as a dataset that doesn't load (ADR 0009: a text is a message enum member).
/// </summary>
public enum EvalErrorMessage
{
    [Description("Dataset '{Dataset}' has the scenario id '{Id}' twice.")]
    DuplicateScenarioId,

    [Description("Scenario '{Id}': its input must be one question from the user (one message whose role is "
                 + "user); a follow-up goes in metadata.follow_ups.")]
    InputNotOneQuestion,

    [Description("the protocol's lines have no mode line, so the conversation's mode is unknown.")]
    NoModeLine,

    [Description("transcript file '{File}' is neither a reference nor a known-bad of one of the eval's scenarios.")]
    StrayTranscript,

    [Description("it holds no JSON object")]
    EmptyFile,

    [Description("Dataset manifest '{File}' is not valid: {Reason}.")]
    InvalidManifest,

    [Description("Scenario file '{File}' is not valid: {Reason}.")]
    InvalidScenario,

    [Description("Scenario '{Id}' has no object of success criteria in 'expected'.")]
    ExpectedNotAnObject,

    [Description("Scenario '{Id}' expects '{Key}', which no grader reads.")]
    UnreadExpectedKey,

    [Description("Folder '{Folder}' in scenarios/ is neither dev nor held-out.")]
    UnknownSplitFolder,

    [Description("Scenario file '{File}' must sit in dev/ or held-out/, named in lowercase ending .json.")]
    UnexpectedFile,

    [Description("Dataset folder '{Folder}' has no dataset.json.")]
    NoManifest,

    [Description("Dataset '{Dataset}' is at version {Version}, so changes needs {Version} dated line(s), not "
                 + "{Count}: one per version, from 1.")]
    ChangesNotVersion,

    [Description("Dataset '{Dataset}': the scenarios hash to {Hash}, not content_hash {Recorded}. If a scenario "
                 + "changed, bump the version and add a dated line to changes; then set content_hash to {Hash}.")]
    HashChanged,

    [Description("The API answered {Status}: {Reason}")]
    ApiRefused,

    [Description(
        "Dataset '{Dataset}': a grader reads '{Key}', which no scenario sets in expected, so it would grade nothing.")]
    UnfedKey,

    [Description("The stored run has no first line naming its eval, so there is nothing to re-score.")]
    NoBeginLine,

    [Description("A re-score opens no session and calls no model, so '{Call}' was never meant to be reached.")]
    NoSessionInARescore,

    [Description("Label file '{File}' is not valid: {Reason}.")]
    InvalidLabel,

    [Description("Label file '{File}' has no label yet. Give it a label (\"pass\" true or false, and a note), or "
                 + "delete the file.")]
    LabelNotGiven,

    [Description("Label file '{File}' is a label for the grader '{Grader}', which the eval does not have. Its "
                 + "graders are {Graders}.")]
    LabelGraderUnknown,

    [Description("Label file '{File}' labels the scenario '{Scenario}', which the dataset does not have.")]
    LabelScenarioUnknown,

    [Description("Label file '{File}' labels turn {Turn}, which scenario '{Scenario}' does not have.")]
    LabelTurnUnknown,

    [Description("Grader '{Grader}' is not a model grader, so there is no judge for a person to be checked against.")]
    ReviewNeedsAModelGrader,

    [Description("The judge prompt has no title line ('# ...'), so there is no question to ask a person.")]
    PromptHasNoTitle,

    [Description("EVAL_JUDGE_PROVIDER '{Value}' is not supported. Use one of: {Supported}.")]
    UnknownJudgeProvider,
}
