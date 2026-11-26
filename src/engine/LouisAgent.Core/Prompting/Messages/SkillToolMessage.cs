using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Prompting.Messages.Holes.Arguments;
using LouisAgent.Core.Prompting.Messages.Holes.Door;
using LouisAgent.Core.Prompting.Messages.Holes.Drafting;
using LouisAgent.Core.Prompting.Messages.Holes.Library;
using LouisAgent.Core.Prompting.Messages.Holes.Running;

namespace LouisAgent.Core.Prompting.Messages;

/// <summary>
/// What a skill's tools answer the model (F97.1): a call that doesn't fit the skill's parameters, each problem named
/// so the call can be fixed in one go, and a run that ran too long or printed nothing. Pinned in the tool-message
/// golden file.
/// </summary>
public enum SkillToolMessage
{
    [Description("The arguments must be a JSON object, one property for each of the skill's parameters.")]
    ArgumentsNotAnObject,
    [Description("'{Parameter}' is required, and the call left it out.")]
    [Holes(typeof(RequiredArgumentLeftOutHoles))]
    RequiredArgumentLeftOut,
    [Description("'{Parameter}' isn't one of this skill's parameters, which are {Known}.")]
    [Holes(typeof(ArgumentUnknownHoles))]
    ArgumentUnknown,
    [Description("'{Parameter}' must be {Expected}, and the call gave {Given}.")]
    [Holes(typeof(ArgumentWrongTypeHoles))]
    ArgumentWrongType,
    [Description("{Skill} ran past its {Seconds} seconds and was stopped. The end of what it printed: {Output}")]
    [Holes(typeof(SkillTimedOutHoles))]
    SkillTimedOut,
    [Description("{Skill} ran and printed nothing.")]
    [Holes(typeof(SkillRanNoOutputHoles))]
    SkillRanNoOutput,
    [Description("- {Name}: {Description}")]
    [Holes(typeof(SkillListedHoles))]
    SkillListed,
    [Description("No skills are approved yet.")]
    NoSkillsApproved,
    [Description(
        "'{Name}' can't be a skill's name: use lowercase letters, digits and hyphens, at most {Longest} characters, " +
        "such as word-count.")]
    [Holes(typeof(SkillNameInvalidHoles))]
    SkillNameInvalid,
    [Description("An approved skill is called {Name} already: give this one another name.")]
    [Holes(typeof(SkillAlreadyApprovedHoles))]
    SkillAlreadyApproved,
    [Description("{Skill}'s example doesn't fit its parameters, so it was kept nowhere: {Problems}")]
    [Holes(typeof(ExampleDoesNotFitHoles))]
    ExampleDoesNotFit,
    [Description("{Skill}'s example didn't run cleanly, so it was kept nowhere. Fix it and make it again: {Answer}")]
    [Holes(typeof(ExampleFailedHoles))]
    ExampleFailed,
    [Description("{Skill} is drafted, for this chat only until it is approved. Its example answered: {Answer}")]
    [Holes(typeof(SkillDraftedHoles))]
    SkillDrafted,
    [Description(
        "{Name}: {Description}\n\n{Instructions}\n\nIts parameters, a JSON Schema: {Parameters}\n\n" +
        "The call it was proved with, and its answer: {Example}")]
    [Holes(typeof(SkillReadHoles))]
    SkillRead,
    [Description("No example was kept.")]
    NoExampleKept,
    [Description("{Skill} didn't run, since the call doesn't fit it: {Problems} Its parameters: {Parameters}")]
    [Holes(typeof(ArgumentsDoNotFitHoles))]
    ArgumentsDoNotFit,
    [Description(
        "The person approved {Name}: it is approved for every chat, and this chat can keep using it through UseSkill.")]
    [Holes(typeof(DraftDecidedHoles))]
    DraftApprovedNote,
    [Description("The person rejected {Name}: its draft is deleted, and no chat can use it.")]
    [Holes(typeof(DraftDecidedHoles))]
    DraftRejectedNote,
    [Description("<system-reminder>\n{Notes}\n</system-reminder>\n\n")]
    [Holes(typeof(SystemReminderHoles))]
    SystemReminder,
    [Description("No skill this chat may use is called {Name}. The ones it may use: {Available}.")]
    [Holes(typeof(NoSuchSkillForThisChatHoles))]
    NoSuchSkillForThisChat,
}
