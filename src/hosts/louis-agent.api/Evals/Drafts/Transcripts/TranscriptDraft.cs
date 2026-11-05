using LouisAgent.Composition.Messages;
using LouisAgent.Api.Messages;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Evals.Drafts.Transcripts;

/// <summary>What a kept transcript stands for: a reference answer, or a known-bad one with its reason.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum TranscriptKind
{
    [WireName("reference")] Reference,
    [WireName("bad")] Bad,
}

/// <summary>
/// A reviewed conversation kept as a transcript, waiting to be published: which eval and identity it is for, the run
/// and conversation it came from, a reference or a known-bad and why, the file name it will have, its protocol lines
/// exactly as the run recorded them, who kept it and when.
/// </summary>
/// <remarks>
/// A draft belongs to one eval (ADR 0033), and outlives its run: deleting the run leaves it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
public sealed record TranscriptDraft(
    string Id,
    string Eval,
    string Identity,
    string Run,
    ConversationKey Conversation,
    TranscriptKind Kind,
    string? Reason,
    string FileName,
    IReadOnlyList<string> Lines,
    string Author,
    DateTimeOffset At)
{
    /// <summary>
    /// The name the eval folders give a transcript: <c>cook-af.reference.txt</c>, or <c>cook-af.bad.side-dish.txt</c>
    /// for a known-bad, which can't be kept without its reason.
    /// </summary>
    /// <remarks>Guard clauses (ADR 0051): a reference first, then a known-bad without its reason refused.</remarks>
    public static string FileNameOf(string scenario, TranscriptKind kind, string? reason)
    {
        if (kind == TranscriptKind.Reference) return $"{scenario}.reference.txt";
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(MessageText.Format(DraftMessage.KnownBadNeedsAReason,
                new { Scenario = scenario }));
        return $"{scenario}.bad.{reason}.txt";
    }
}
