namespace LouisAgent.Api.Messages;

using System.ComponentModel;

/// <summary>What the API answers its clients and the web app: error messages, merge and delete results.</summary>
public enum ApiUserText
{
    [Description("'{Tag}' must be at most {Max} printable characters.")]
    InvalidTag,

    [Description("Unknown time zone '{TimeZone}'. Use an IANA name, e.g. Africa/Johannesburg.")]
    UnknownTimeZone,

    [Description("Missing or invalid API key (x-api-key or Authorization: Bearer).")]
    InvalidApiKey,

    [Description("This API has no route /v1/{Path}.")]
    UnknownRoute,

    [Description("No price table loaded: set PRICES_FILE or add config/prices.json.")]
    NoPriceTable,

    [Description("No exchange rates loaded: RATES_URL couldn't be used and there is no config/rates.json.")]
    NoRates,

    [Description("Send a non-empty 'message' and/or 'attachments'.")]
    EmptyMessage,

    [Description("This session is still answering a message; wait for it or POST /v1/sessions/{Id}/cancel.")]
    SessionBusy,

    [Description("Unknown session: {Id}")]
    UnknownSession,

    [Description("Pass the file's workspace-relative 'path'.")]
    PathRequired,

    [Description("Diffs of secret files (.env, keys, certificates) aren't shown.")]
    SecretDiffHidden,

    [Description("Write a commit message.")]
    CommitMessageRequired,

    [Description("Nothing is staged: stage the changes you want to commit first.")]
    NothingStaged,

    [Description("Pass at least one path in 'paths'.")]
    PathsRequired,

    [Description("{Path}: {Reason}")]
    PathRefused,

    [Description("No folder at '{Path}'.")]
    NoFolder,

    [Description("No file at '{Path}'.")]
    NoFile,

    [Description("Secret files (.env, keys, certificates) can't be opened.")]
    SecretFileHidden,

    [Description("Pass the commit's 'sha' (7-40 hex characters).")]
    ShaRequired,

    [Description("No branch named '{Name}'.")]
    NoBranch,

    [Description("'{Branch}' is the checked-out branch.")]
    MergeCheckedOutBranch,

    [Description("'{Branch}' is already merged into {Current}.")]
    MergeAlreadyMerged,

    [Description("{Current} has uncommitted changes: commit, unstage or discard them in Changes before merging.")]
    MergeUncommittedChanges,

    [Description("Merge branch '{Branch}' into {Current}")]
    MergeCommitMessage,

    [Description("git has no author identity: set GIT_AUTHOR_NAME, GIT_AUTHOR_EMAIL, GIT_COMMITTER_NAME and GIT_COMMITTER_EMAIL and restart the api.")]
    MergeNoIdentity,

    [Description("Merging '{Branch}' conflicts in {Files}; the merge was aborted, nothing changed.")]
    MergeConflicts,

    [Description("git could not merge '{Branch}': {Detail}")]
    MergeFailed,

    [Description("Merged {Branch} into {Current}: {Head}")]
    Merged,

    [Description("Deleted {Branch}")]
    BranchDeleted,

    [Description("Binary files differ (secret file: diff not shown)\n")]
    SecretDiffBinary,

    [Description("git has no author identity in the container: set GIT_AUTHOR_NAME, GIT_AUTHOR_EMAIL, GIT_COMMITTER_NAME and GIT_COMMITTER_EMAIL in config/.env.secrets and restart the api container.")]
    CommitNoIdentity,

    [Description("git exited with code {ExitCode}.")]
    GitExited,

    [Description("git timed out after {Seconds}s.")]
    GitTimedOut,

    [Description("Can't delete the checked-out branch.")]
    DeleteCurrentBranch,

    [Description("'{Branch}' isn't merged yet; merge it first.")]
    BranchNotMerged,

    [Description("The admin portal is closed: set ADMIN_API_KEY (or AGENT_API_KEY) in config/.env.secrets and recreate the container.")]
    AdminClosed,

    [Description("No check named '{Check}'. The checks: {Checks}.")]
    NoCheck,

    [Description("A run of {Check} is in progress; wait for it to end. The containers are shared, so one runs at a time.")]
    BenchmarkRunning,

    [Description("No run in progress.")]
    NoBenchmarkRun,

    [Description("The run ended before this turn did.")]
    BenchmarkTurnCutOff,

    [Description("The store stopped taking writes ({Reason}): what came after isn't kept, but the run went on.")]
    BenchmarkStoreFailed,

    [Description("The check speaks protocol {Version} and this API reads {Supported}: lines it doesn't know are kept as the log.")]
    BenchmarkProtocolUnknown,

    [Description("The usage ledger couldn't be read ({Reason}), so the run's cost is unknown.")]
    BenchmarkLedgerUnreadable,

    [Description("The benchmark store isn't set, so a run's results would be lost: set BENCHMARKS_MONGO_URL in config/.env.secrets and recreate the container.")]
    NoBenchmarkStore,

    [Description("No stored run '{Id}'.")]
    NoStoredRun,

    [Description("Run {Id} is a run of '{Check}', which the eval runner does not list, so there are no transcripts to grade again.")]
    RescoreNeedsEval,

    [Description("Run {Id} is still running: delete it once it has ended.")]
    StoredRunStillRunning,

    [Description("The benchmark store didn't answer: {Reason}")]
    BenchmarkStoreUnavailable,

    [Description("The check '{Check}' has no manual: its eval lists none, or the file it names isn't under evals/.")]
    NoManual,

    [Description("Unknown stream format '{Format}'. Use one of: {Supported}.")]
    UnknownStreamFormat,

    [Description("'replay' is only for an eval session: send a 'run' tag with it. A live chat is never recorded or replayed.")]
    ReplayNeedsRunTag,

    [Description("Unknown replay.web '{Value}'. Use one of: {Supported}.")]
    UnknownReplayWeb,

    [Description("replay.model is only for web 'replay': the model's answers are replayed from a recording, so send replay.web 'replay' with it (and 'of').")]
    ReplayModelNeedsReplay,

    [Description("This instance keeps no recordings, so it can't record or replay web pages: its replay folder isn't set (config/.env.example says how).")]
    ReplayNoRecordings,

    [Description("replay.web 'replay' needs 'of': the recorded trial to read its pages from.")]
    ReplayNeedsOf,

    [Description("The ledger's rows name model '{Model}', which the run's modes don't list, so a trend may mix models.")]
    BenchmarkModelUnlisted,

    [Description("Send web or replay, not both: a replay is a recorded run's web again, and a record is a new one.")]
    RunWebAndReplay,

    [Description("Unknown web '{Value}'. Use one of: {Supported}.")]
    RunBadWeb,

    [Description("web can't be 'replay' on its own: a replay names its run, so send replay with the recorded run's id.")]
    RunWebIsReplay,

    [Description("replay takes a run id's letters, digits, '-' and '_', not '{Value}'.")]
    RunBadReplay,

    [Description("Run {Run} is not a judge check, so there is no report to keep.")]
    CalibrationNotACheck,

    [Description("The judge check ended with exit code {Code} and no report. Its last line: {Last}")]
    CalibrationRunFailed,

    [Description("The judge check ended without its report line. Its last line: {Last}")]
    CalibrationNoReport,
}
