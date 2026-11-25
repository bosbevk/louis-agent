using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Orchestrator.Messages.Holes.Checks;
using LouisAgent.Orchestrator.Messages.Holes.Comms;
using LouisAgent.Orchestrator.Messages.Holes.Events;
using LouisAgent.Orchestrator.Messages.Holes.Fixes;
using LouisAgent.Orchestrator.Messages.Holes.Startup;

namespace LouisAgent.Orchestrator.Messages;

/// <summary>The orchestrator's log lines; written to stderr (and the run file), tagged [orchestrator].</summary>
public enum OrchestratorLogMessage
{
    [Description("Costs shown in {Display}")]
    [Holes(typeof(CostsShownInHoles))]
    CostsShownIn,

    [Description("Watching {Service}: {ErrorLog}")]
    [Holes(typeof(WatchingHoles))]
    Watching,

    [Description("Runbooks for: {Methods}; louis-agent at {Url}; model {Model}")]
    [Holes(typeof(RunbooksHoles))]
    Runbooks,

    [Description("Agent communications log: {Path}")]
    [Holes(typeof(CommsLogAtHoles))]
    CommsLogAt,

    [Description("louis-agent health check returned {Status}")]
    [Holes(typeof(HealthCheckFailedHoles))]
    HealthCheckFailed,

    [Description("louis-agent is not reachable ({Reason}); fixes will fail until it is started")]
    [Holes(typeof(LouisAgentUnreachableHoles))]
    LouisAgentUnreachable,

    [Description("{Count} new error event(s); usage run id {RunId}")]
    [Holes(typeof(NewEventsHoles))]
    NewEvents,

    [Description("--- {Number}/{Count} {ErrorId}: {Method} {Arguments} -> {ExceptionType}: {Message}")]
    [Holes(typeof(EventStartedHoles))]
    EventStarted,

    [Description("Triage of {ErrorId} failed: {Reason}")]
    [Holes(typeof(TriageFailedHoles))]
    TriageFailed,

    [Description("Not back on the main branch: {Problem}")]
    [Holes(typeof(NotBackOnMainHoles))]
    NotBackOnMain,

    [Description("DECISION {ErrorId}: {Outcome} - {Reason}")]
    [Holes(typeof(DecisionHoles))]
    Decision,

    [Description("USAGE {ErrorId}: {Usage}")]
    [Holes(typeof(EventUsageHoles))]
    EventUsage,

    [Description("Agent communications written to {Path}")]
    [Holes(typeof(CommsWrittenHoles))]
    CommsWritten,

    [Description("Calling louis-agent at {Url} to fix {ExceptionType} in {Method} (branch {Branch})")]
    [Holes(typeof(CallingLouisAgentHoles))]
    CallingLouisAgent,

    [Description("louis-agent stopped without a FIX-RESULT; asking it to continue")]
    AskingToContinue,

    [Description("louis-agent replied: {Status}")]
    [Holes(typeof(LouisAgentRepliedHoles))]
    LouisAgentReplied,

    [Description("louis-agent replied: {Status}, commit {Commit} on {Branch}")]
    [Holes(typeof(LouisAgentCommittedHoles))]
    LouisAgentCommitted,

    [Description("  {Line}")]
    [Holes(typeof(LouisAgentProgressHoles))]
    LouisAgentProgress,

    [Description("Verifying commit {Commit} on {Branch}")]
    [Holes(typeof(VerifyingCommitHoles))]
    VerifyingCommit,

    [Description("  {Line}")]
    [Holes(typeof(ResultHoles))]
    Result,

    [Description("Replaying {Method} {OrderId}")]
    [Holes(typeof(ReplayingHoles))]
    Replaying,

    [Description("Running the service's tests")]
    RunningTests,

    [Description("ESCALATED {ErrorId} to {Team}: {Reason}")]
    [Holes(typeof(EscalatedHoles))]
    Escalated,

    [Description("Acknowledged {ErrorId}: {Reason}")]
    [Holes(typeof(AcknowledgedHoles))]
    Acknowledged,

    [Description("Stashed uncommitted changes left on {Branch}")]
    [Holes(typeof(StashedHoles))]
    Stashed,

    [Description("-> {Tool}")]
    [Holes(typeof(LouisAgentToolCallHoles))]
    LouisAgentToolCall,

    [Description("   (tool reported an error)")]
    LouisAgentToolFailed,
}
