namespace LouisAgent.Api.Contracts.Admin.Studio;

/// <summary>Every eval being composed in the studio, oldest first.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record EvalDraftsReply(IReadOnlyList<EvalDraftRow> Drafts);

/// <summary>
/// An eval being composed: its id, the identity and name read from its file, the file's text as typed, its rubrics,
/// who saved it last and when, and the scenario files of its own a promptfoo import brought.
/// </summary>
public sealed record EvalDraftRow(
    string Id,
    string Identity,
    string Name,
    string Json,
    IReadOnlyList<RubricRow> Rubrics,
    string Author,
    DateTimeOffset At,
    IReadOnlyList<RubricRow> Scenarios);

/// <summary>What the studio's import page sends: a promptfoo file's text and who is importing it.</summary>
public sealed record PromptfooImportRequest(string? Yaml, string? Author);

/// <summary>
/// What a promptfoo import made: the new eval draft, and each part of the file that has no counterpart here, in the
/// runner's words, so the person knows what to add by hand.
/// </summary>
public sealed record PromptfooImportReply(EvalDraftRow Draft, IReadOnlyList<string> LeftOut);

/// <summary>
/// What the compose page sends to try a draft: the stored run to replay (a try is always a replay, so it costs
/// nothing) and the samples to run, none for all of them.
/// </summary>
public sealed record TryRequest(string? Replay, IReadOnlyList<string>? Samples);

/// <summary>
/// What a publish made: the branch the eval's folder was committed on, the commit, and the folder's path in the
/// repository. The push and the pull request are the person's.
/// </summary>
public sealed record PublishReply(string Branch, string Commit, string Folder);

/// <summary>
/// What the eval runner said of a draft written out as a folder: whether it conforms, and every problem in the runner's
/// own words, in the order a run would meet them.
/// </summary>
public sealed record DraftCheckReply(bool Ok, IReadOnlyList<string> Problems);

/// <summary>One rubric: its file's name in <c>Rubrics/</c> and its text.</summary>
public sealed record RubricRow(string Name, string Text);

/// <summary>
/// What the compose page sends to save an eval: its file's text (which names the eval and its identity), its rubrics,
/// who is saving it and the scenario files of its own it keeps.
/// </summary>
public sealed record EvalDraftRequest(
    string? Json,
    IReadOnlyList<RubricRow>? Rubrics,
    string? Author,
    IReadOnlyList<RubricRow>? Scenarios);
