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

    [Description("No price table loaded: set PRICES_FILE or add config/prices.json.")]
    NoPriceTable,

    [Description("No exchange rates loaded: RATES_URL couldn't be used and there is no config/rates.json.")]
    NoRates,

    [Description("Send a non-empty 'message' and/or 'attachments'.")]
    EmptyMessage,

    [Description("This session is still answering a message; wait for it or POST /sessions/{Id}/cancel.")]
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

    [Description("Can't delete the checked-out branch.")]
    DeleteCurrentBranch,

    [Description("'{Branch}' isn't merged yet; merge it first.")]
    BranchNotMerged,
}
