using System.Text.Json;
using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Api.Contracts.Admin.Drafts;

/// <summary>An eval's drafts, oldest first: the samples and the transcripts people made from its runs.</summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA): a plain shape both sides read. The API builds it with <c>DraftsReply.Of</c>,
/// beside its own draft records (<c>DraftReplies</c>), so this project needs none of them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record DraftsReply(IReadOnlyList<SampleDraftRow> Samples, IReadOnlyList<TranscriptDraftRow> Transcripts);

/// <summary>
/// A sample draft: which eval it is for, the run and conversation it came from, the sample's id and first question,
/// the scenario file it will be as JSON, who made it and when.
/// </summary>
public sealed record SampleDraftRow(
    string Id,
    string Eval,
    string Identity,
    string Run,
    ConversationKeyRow Conversation,
    string SampleId,
    string Question,
    string Json,
    string Author,
    DateTimeOffset At);

/// <summary>
/// A transcript draft: which eval it is for, the run and conversation it came from, a reference or a known-bad and
/// why, the file name it will have, its protocol lines as one text, who kept it and when.
/// </summary>
public sealed record TranscriptDraftRow(
    string Id,
    string Eval,
    string Identity,
    string Run,
    ConversationKeyRow Conversation,
    string Kind,
    string? Reason,
    string FileName,
    int Lines,
    string Text,
    string Author,
    DateTimeOffset At);

/// <summary>
/// The scenario a conversation ran, as the sample dialog starts from it: its id, place and time zone, its expected
/// values and its categories.
/// </summary>
/// <remarks>The expected values stay JSON: a key may hold text, a number, a list or null.</remarks>
public sealed record ScenarioRow(
    string Id,
    string? Place,
    string TimeZone,
    JsonElement Expected,
    IReadOnlyList<string> Categories);

/// <summary>
/// What the portal sends to make a sample from one of a run's conversations: the sample's id, the expected values that
/// change (an object of only those keys), the categories when the person picks their own, an ideal answer, and who
/// made it.
/// </summary>
public sealed record SampleDraftRequest(
    ConversationKeyRow? Conversation,
    string? Id,
    JsonElement? Expected,
    IReadOnlyList<string>? Categories,
    string? Target,
    string? Author);

/// <summary>
/// What the portal sends to keep a reviewed conversation as a transcript: <c>reference</c> or <c>bad</c>, a known-bad's
/// reason in kebab-case, and who kept it.
/// </summary>
public sealed record TranscriptDraftRequest(
    ConversationKeyRow? Conversation,
    string? Kind,
    string? Reason,
    string? Author);
