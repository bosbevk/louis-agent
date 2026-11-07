using System.ComponentModel;

namespace LouisAgent.Api.Evals.Messages;

/// <summary>What the studio's routes tell the person: a store that isn't there, a run that can't start, a name that isn't known.</summary>
public enum EvalApiUserText
{
    [Description("No check named '{Check}'. The checks: {Checks}.")]
    NoCheck,

    [Description(
        "A run of {Check} is in progress; wait for it to end. The containers are shared, so one runs at a time.")]
    BenchmarkRunning,

    [Description("The run ended before this turn did.")]
    BenchmarkTurnCutOff,

    [Description("The store stopped taking writes ({Reason}): what came after isn't kept, but the run went on.")]
    BenchmarkStoreFailed,

    [Description(
        "The check speaks protocol {Version} and this API reads {Supported}: lines it doesn't know are kept as the "
        + "log.")]
    BenchmarkProtocolUnknown,

    [Description("The usage ledger couldn't be read ({Reason}), so the run's cost is unknown.")]
    BenchmarkLedgerUnreadable,

    [Description(
        "The eval store isn't set, so a run's results would be lost: set EVALS_MONGO_URL in "
        + "config/.env.secrets and recreate the container.")]
    NoBenchmarkStore,

    [Description(
        "Run {Id} is a run of '{Check}', which the eval runner does not list, so there are no transcripts to grade "
        + "again.")]
    RescoreNeedsEval,

    [Description("Run {Id} is still running: delete it once it has ended.")]
    StoredRunStillRunning,

    [Description("The benchmark store didn't answer: {Reason}")]
    BenchmarkStoreUnavailable,

    [Description("The check '{Check}' has no manual: its eval lists none, or the file it names isn't under evals/.")]
    NoManual,

    [Description(
        "The ledger's rows name model '{Model}', which the run's modes don't list, so a trend may mix models.")]
    BenchmarkModelUnlisted,

    [Description("Send web or replay, not both: a replay is a recorded run's web again, and a record is a new one.")]
    RunWebAndReplay,

    [Description("Unknown web '{Value}'. Use one of: {Supported}.")]
    RunBadWeb,

    [Description(
        "web can't be 'replay' on its own: a replay names its run, so send replay with the recorded run's id.")]
    RunWebIsReplay,

    [Description("replay takes a run id's letters, digits, '-' and '_', not '{Value}'.")]
    RunBadReplay,

    [Description("a stored run names a {Field} that is not known: '{Value}'.")]
    StoredNameUnknown,

    [Description("Run {Run} is not a judge check, so there is no report to keep.")]
    CalibrationNotACheck,

    [Description("The judge check ended with exit code {Code} and no report. Its last line: {Last}")]
    CalibrationRunFailed,

    [Description("The judge check ended without its report line. Its last line: {Last}")]
    CalibrationNoReport,
}
