using System.ComponentModel;

namespace LouisAgent.Api.Messages;

/// <summary>
/// The texts of the drafts a person makes in the admin portal: what a draft can't be made without (ADR 0009).
/// </summary>
public enum DraftMessage
{
    [Description(
        "A known-bad transcript needs a reason, in kebab-case: its file is named {Scenario}.bad.<reason>.txt, so the name says what went wrong.")]
    KnownBadNeedsAReason,

    [Description("Conversation {Mode}/{Repeat}/{Task} has no turns, so there is no question to make a sample of.")]
    ConversationHasNoTurns,

    [Description("The expected values are a JSON object of only the keys that change, each with its new value.")]
    ExpectedNotAnObject,

    [Description(
        "'{Key}' is not an expected key of scenario {Scenario}, so no check would read it. Its keys are: {Keys}.")]
    ExpectedKeyUnknown,

    [Description(
        "The sample's id '{Id}' must be kebab-case (lowercase letters and digits joined by single dashes): it names the scenario's file.")]
    SampleIdNotKebabCase,

    [Description(
        "The reason '{Reason}' must be kebab-case (lowercase letters and digits joined by single dashes): it becomes part of the transcript's file name.")]
    ReasonNotKebabCase,

    [Description("Name the conversation the draft comes from: its mode, repeat and task.")]
    NeedsAConversation,

    [Description("Say who made the draft: the author is blank.")]
    AuthorBlank,

    [Description(
        "Eval '{Eval}' has no scenario '{Scenario}' in its folder, so there is nothing to make a sample from.")]
    NoScenario,

    [Description("The runner's catalogue has no eval '{Eval}'.")]
    NoEval,

    [Description("Eval '{Eval}' has no draft '{Id}'.")]
    NoDraft,

    [Description("'{Kind}' is not a kind of transcript. Supported: {Supported}.")]
    TranscriptKindUnknown,

    [Description(
        "Nobody has passed or failed conversation {Mode}/{Repeat}/{Task} yet, so it can be kept as neither a reference nor a known-bad.")]
    NotDecided,

    [Description(
        "A reference needs every person's verdict on {Task} to be a pass, and it has a fail: keep it as a known-bad instead.")]
    ReferenceNeedsPasses,

    [Description(
        "A known-bad needs a person's fail on {Task}, and it has only passes: keep it as a reference instead.")]
    KnownBadNeedsAFail,

    [Description("The eval's file must be a JSON object that names the eval and the identity it proves.")]
    EvalFileNotAnObject,

    [Description(
        "The eval's file needs its '{Field}' as text in kebab-case (lowercase letters and digits joined by single dashes): it names the folder the eval becomes.")]
    EvalFileNeedsAName,

    [Description("The eval's file is missing, or longer than {Limit} characters.")]
    EvalFileTooBig,

    [Description("An eval takes at most {Limit} rubrics.")]
    TooManyRubrics,

    [Description("The rubric '{Name}' must be a kebab-case name ending in .md, such as meal-for-people.md.")]
    RubricNameNotKebabCase,

    [Description("The rubric '{Name}' is longer than {Limit} characters.")]
    RubricTooBig,

    [Description("There is no eval being composed with the id '{Id}'.")]
    NoEvalDraft,
}
