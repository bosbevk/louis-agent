namespace LouisAgent.Orchestrator.Messages;

using System.ComponentModel;

/// <summary>What the orchestrator's tools return to its model, and the messages they send louis-agent.</summary>
public enum OrchestratorToolMessage
{
    [Description("Error: unknown error id '{ErrorId}'.")]
    UnknownErrorId,

    [Description("Error: a fix for {ErrorId} was already requested; verify that one instead.")]
    FixAlreadyRequested,

    [Description("Error: can't start the fix: {Reason}")]
    FixCannotStart,

    [Description("Error: commit must be 7-40 hexadecimal characters.")]
    InvalidCommitHash,

    [Description("Error: invalid branch name.")]
    InvalidBranchName,

    [Description("Error: unknown method '{Method}'. Known: {Known}.")]
    UnknownMethod,

    [Description("Escalated {ErrorId} to the {Team} team.")]
    Escalated,

    [Description("Acknowledged {ErrorId}; no action.")]
    Acknowledged,

    [Description("commit exists")]
    CheckCommitExists,

    [Description("fix branch is checked out")]
    CheckBranchCheckedOut,

    [Description("HEAD is {Branch}")]
    CheckHeadIs,

    [Description("commit is on {Branch}")]
    CheckCommitOnBranch,

    [Description("commit is not on {Branch}")]
    CheckCommitNotOnMain,

    [Description("branch starts from the tip of {Branch}")]
    CheckBranchFromMainTip,

    [Description("commit changes files")]
    CheckCommitChangesFiles,

    [Description("working tree is clean")]
    CheckWorkingTreeClean,

    [Description("exit code {ExitCode}{TimedOut}\n{Output}")]
    ExitCodeAndOutput,

    [Description("exit code {ExitCode}")]
    ExitCode,

    [Description("orchestrator: uncommitted changes left on {Branch}")]
    StashMessage,

    [Description("could not stash the uncommitted changes on {Branch}: {Output}")]
    StashFailed,

    [Description("could not check out {Branch}: {Output}")]
    CheckoutMainFailed,

    [Description("HTTP {Status}: {Body}")]
    HttpFailed,

    [Description(" (timed out)")]
    TimedOutSuffix,

    [Description("Continue the fix from where you stopped. Finish with the FIX-RESULT line.")]
    ContinueFix,
}
