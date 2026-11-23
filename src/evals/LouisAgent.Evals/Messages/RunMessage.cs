using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Messages.Holes.Runs;

namespace LouisAgent.Evals.Messages;

/// <summary>
/// Why a run couldn't finish: each says the harness failed, not the agent (ADR 0009: a text is a member of a message
/// enum, its <c>[Description]</c> a template with named holes).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum RunMessage
{
    [Description("The judge couldn't mark a criterion, so nothing more could be learnt about the agent: {Reason}")]
    [Holes(typeof(JudgeFailedHoles))]
    JudgeFailed,

    [Description("The sessions API refused, so the agent couldn't be asked: {Reason}")]
    [Holes(typeof(SessionRefusedHoles))]
    SessionRefused,

    [Description("The recording no longer answers, so it is out of date and a live run is needed: {Changed}")]
    [Holes(typeof(RecordingOutOfDateHoles))]
    RecordingOutOfDate,

    [Description("The page {Address} that a conversation names is not a page of this site, so the agent couldn't " +
                 "be asked with it.")]
    [Holes(typeof(PageNotOnTheSiteHoles))]
    PageNotOnTheSite,

    [Description("'{Id}' is no run's id. A run's id is the UTC time it started and its eval's name, as " +
                 "20261123-043012-guide.")]
    [Holes(typeof(NotARunIdHoles))]
    NotARunId,

    [Description("'{File}' in the runs' folder is no run: {Reason}")]
    [Holes(typeof(NotARunHoles))]
    NotARun,

    [Description("Its name is no run's id.")]
    NotNamedForARun,

    [Description("It holds no run.")] HoldsNoRun
}
