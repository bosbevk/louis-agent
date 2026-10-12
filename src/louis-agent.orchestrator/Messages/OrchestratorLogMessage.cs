namespace LouisAgent.Orchestrator.Messages;

using System.ComponentModel;

/// <summary>The orchestrator's log lines; written to stderr (and the run file), tagged [orchestrator].</summary>
public enum OrchestratorLogMessage
{
    [Description("Costs shown in {Display}")]
    CostsShownIn,

    [Description("Watching {Service}: {ErrorLog}")]
    Watching,

    [Description("Runbooks for: {Methods}; louis-agent at {Url}; model {Model}")]
    Runbooks,

    [Description("Agent communications log: {Path}")]
    CommsLogAt,

    [Description("louis-agent health check returned {Status}")]
    HealthCheckFailed,

    [Description("louis-agent is not reachable ({Reason}); fixes will fail until it is started")]
    LouisAgentUnreachable,

    [Description("{Count} new error event(s); usage run id {RunId}")]
    NewEvents,

    [Description("--- {Number}/{Count} {ErrorId}: {Method} {Arguments} -> {ExceptionType}: {Message}")]
    EventStarted,

    [Description("Triage of {ErrorId} failed: {Reason}")]
    TriageFailed,

    [Description("Not back on the main branch: {Problem}")]
    NotBackOnMain,

    [Description("DECISION {ErrorId}: {Outcome} - {Reason}")]
    Decision,

    [Description("USAGE {ErrorId}: {Usage}")]
    EventUsage,

    [Description("Agent communications written to {Path}")]
    CommsWritten,

    [Description("Calling louis-agent at {Url} to fix {ExceptionType} in {Method} (branch {Branch})")]
    CallingLouisAgent,

    [Description("louis-agent stopped without a FIX-RESULT; asking it to continue")]
    AskingToContinue,

    [Description("louis-agent replied: {Status}")]
    LouisAgentReplied,

    [Description("louis-agent replied: {Status}, commit {Commit} on {Branch}")]
    LouisAgentCommitted,

    [Description("  {Line}")]
    LouisAgentProgress,

    [Description("Verifying commit {Commit} on {Branch}")]
    VerifyingCommit,

    [Description("  {Line}")]
    Result,

    [Description("Replaying {Method} {OrderId}")]
    Replaying,

    [Description("Running the service's tests")]
    RunningTests,

    [Description("ESCALATED {ErrorId} to {Team}: {Reason}")]
    Escalated,

    [Description("Acknowledged {ErrorId}: {Reason}")]
    Acknowledged,

    [Description("Stashed uncommitted changes left on {Branch}")]
    Stashed,

    [Description("-> {Tool}")]
    LouisAgentToolCall,

    [Description("   (tool reported an error)")]
    LouisAgentToolFailed,
}
