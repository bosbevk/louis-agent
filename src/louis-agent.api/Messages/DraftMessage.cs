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
}
