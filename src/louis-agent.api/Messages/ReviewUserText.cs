using System.ComponentModel;

namespace LouisAgent.Api.Messages;

/// <summary>The texts of the review routes (F18 step 6a.2), apart from the admin's other texts so each enum stays a size a person reads: a verdict, a reviewer, a grader or a label export the request got wrong (ADR 0009).</summary>
public enum ReviewUserText
{
    [Description("'{Verdict}' is not a verdict. Use one of: {Supported}.")]
    ReviewVerdictUnknown,

    [Description("A review needs the name of who gave it.")]
    ReviewerBlank,

    [Description("A reviewer's name is at most {Limit} characters; this one has {Length}.")]
    ReviewerTooLong,

    [Description("A review's note is at most {Limit} characters; this one has {Length}.")]
    NoteTooLong,

    [Description("'{Grader}' is not a human review of run {Id}. Its human reviews are: {Graders}.")]
    ReviewGraderNotHuman,

    [Description("Run {Id} has no conversation {Mode}/{Repeat}/{Task}.")]
    NoStoredConversation,

    [Description(
        "Say which judge's labels to export, as ?grader=. The judges of this run with a human review: {Graders}. (None means the run was made before the human reviews, or its eval has no judge to review.)")]
    LabelsNeedAGrader,

    [Description(
        "No one has passed or failed an answer of {Grader} in run {Id} yet, so there are no labels to export.")]
    NoLabelsYet,

    [Description(
        "'{Grader}' is not a judge with a human review in run {Id}, so there is nothing to check it against. The judges with one: {Graders}.")]
    CalibrationNeedsAJudge,

    [Description(
        "Run {Id} is of '{Check}', which the eval runner does not list: only a listed eval's judge is checked against the people.")]
    CalibrationNeedsEval,
}
