using System.ComponentModel;

namespace LouisAgent.Evals.Messages;

/// <summary>The texts of the eval's refusals, such as a dataset that doesn't load (ADR 0009: a text is a message enum member).</summary>
public enum EvalErrorMessage
{
    [Description("Dataset '{Dataset}' has the scenario id '{Id}' twice.")]
    DuplicateScenarioId,

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

    [Description("Dataset '{Dataset}' is at version {Version}, so changes needs {Version} dated line(s), not {Count}: one per version, from 1.")]
    ChangesNotVersion,

    [Description("Dataset '{Dataset}' changed without a new version: the scenarios hash to {Hash}, not content_hash {Recorded}. Bump the version, add a dated line to changes, and set content_hash to {Hash}.")]
    HashChanged,
}
