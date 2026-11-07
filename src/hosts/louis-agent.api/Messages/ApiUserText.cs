using System.ComponentModel;

namespace LouisAgent.Api.Messages;

/// <summary>What the API answers its clients and the web app: error messages, merge and delete results.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum ApiUserText
{
    [Description("{Name} is larger than 1 MB.")]
    FileTooLarge,

    [Description("{Name} is a binary file.")]
    BinaryFile,

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

    [Description("No feed '{Name}' here; GET /v1/feeds lists the ones there are.")]
    UnknownFeed,

    [Description("The feed '{Name}' has no item '{Id}'.")]
    UnknownFeedItem,

    [Description("No upload '{Name}' here; GET /v1/admin/imports lists the ones there are.")]
    UnknownImport,

    [Description("No screen '{Name}' here; GET /v1/admin/screens lists the ones there are.")]
    UnknownScreen,

    [Description("The screen '{Name}' has no action '{Action}'; GET /v1/admin/screens lists what each can do.")]
    UnknownScreenAction,

    [Description("'{Action}' acts on a row, so it needs a row_id.")]
    ScreenActionNeedsRow,

    [Description("The screen '{Name}' has no row '{Row}'.")]
    UnknownScreenRow,

    [Description("Send the files to upload: 'files', each with its 'slot', 'file_name' and 'content'.")]
    ImportNoFiles,

    [Description("The files come to {Megabytes} MB; an upload takes at most {Max} MB.")]
    ImportTooLarge,

    [Description("Send a non-empty 'message' and/or 'attachments'.")]
    EmptyMessage,

    [Description("This session is still answering a message; wait for it or POST /v1/sessions/{Id}/cancel.")]
    SessionBusy,

    [Description("Unknown session: {Id}")] UnknownSession,

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

    [Description("{Path}: {Reason}")] PathRefused,

    [Description("No folder at '{Path}'.")]
    NoFolder,

    [Description("No file at '{Path}'.")] NoFile,

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

    [Description(
        "git has no author identity: set GIT_AUTHOR_NAME, GIT_AUTHOR_EMAIL, GIT_COMMITTER_NAME and "
        + "GIT_COMMITTER_EMAIL and restart the api.")]
    MergeNoIdentity,

    [Description("Merging '{Branch}' conflicts in {Files}; the merge was aborted, nothing changed.")]
    MergeConflicts,

    [Description("git could not merge '{Branch}': {Detail}")]
    MergeFailed,

    [Description("Merged {Branch} into {Current}: {Head}")]
    Merged,

    [Description("Deleted {Branch}")] BranchDeleted,

    [Description("Binary files differ (secret file: diff not shown)\n")]
    SecretDiffBinary,

    [Description(
        "git has no author identity in the container: set GIT_AUTHOR_NAME, GIT_AUTHOR_EMAIL, GIT_COMMITTER_NAME "
        + "and GIT_COMMITTER_EMAIL in config/.env.secrets and restart the api container.")]
    CommitNoIdentity,

    [Description("git exited with code {ExitCode}.")]
    GitExited,

    [Description("git timed out after {Seconds}s.")]
    GitTimedOut,

    [Description("Can't delete the checked-out branch.")]
    DeleteCurrentBranch,

    [Description("'{Branch}' isn't merged yet; merge it first.")]
    BranchNotMerged,

    [Description(
        "The admin portal is closed: set ADMIN_API_KEY (or AGENT_API_KEY) in config/.env.secrets and recreate the "
        + "container.")]
    AdminClosed,

    [Description("No run in progress.")] NoBenchmarkRun,

    [Description("No stored run '{Id}'.")] NoStoredRun,

    [Description("Unknown stream format '{Format}'. Use one of: {Supported}.")]
    UnknownStreamFormat,

    [Description(
        "'replay' is only for an eval session: send a 'run' tag with it. A live chat is never recorded or replayed.")]
    ReplayNeedsRunTag,

    [Description("Unknown replay.web '{Value}'. Use one of: {Supported}.")]
    UnknownReplayWeb,

    [Description(
        "replay.model is only for web 'replay': the model's answers are replayed from a recording, so send "
        + "replay.web 'replay' with it (and 'of').")]
    ReplayModelNeedsReplay,

    [Description(
        "This instance keeps no recordings, so it can't record or replay web pages: its replay folder isn't set "
        + "(config/.env.example says how).")]
    ReplayNoRecordings,

    [Description("replay.web 'replay' needs 'of': the recorded trial to read its pages from.")]
    ReplayNeedsOf,
}
