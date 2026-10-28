using System.ComponentModel;

namespace LouisAgent.Api.Messages;

/// <summary>The API's log lines; written to stderr.</summary>
public enum ApiLogMessage
{
    [Description("AGENT_AUTH=off: no AGENT_API_KEY, so the API accepts unauthenticated requests, on purpose. "
                 + "Only expose it on localhost.")]
    NoApiKey,

    [Description("Turn failed in {Session}")]
    TurnFailed,

    [Description("Benchmark run {Run}: the store failed, so recording stopped; the run goes on")]
    BenchmarkStoreFailed,

    [Description("{Count} benchmark run(s) a restart cut off are marked failed")]
    BenchmarkOrphansClosed,

    [Description("The benchmark store isn't answering at start-up; a run will say so when it can't write")]
    BenchmarkStoreUnreachable,

    [Description("{Runs} run(s) from before protocol version {Version} deleted at start-up, with "
                 + "{Conversations} conversation(s), {Turns} turn(s), {Requests} request(s), {Notes} note(s), "
                 + "{Grades} grade(s) and {Calibrations} judge check(s)")]
    EvalOldRunsDeleted,

    [Description("The eval runner can't be listed ({Reason}), so the portal has no checks until it can be")]
    BenchmarkListingUnavailable,

    [Description("Judge check of run {Run} ({Grader}) kept no report: {Reason}")]
    CalibrationNotStored,
}
