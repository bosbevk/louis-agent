using LouisAgent.Composition.Messages;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Evals.Drafts.Transcripts;

/// <summary>
/// Keeps a reviewed conversation as a transcript draft, when people's verdicts say what it is: a reference when every
/// decided verdict is a pass, a known-bad when at least one is a fail. A conversation nobody passed or failed (only
/// unsure, or no review at all) offers neither.
/// </summary>
/// <remarks>
/// A static factory (ADR 0040's <c>Of</c>) with guard clauses (ADR 0051), one per refusal, in the order a person
/// would fix them. A reference has to be good in every judge's eyes, since the eval holds it up as the answer; one
/// fail is enough to make a known-bad, since its reason names what that fail was (unattended pick, see the plan).
/// The refusals' messages are what the person reads, so they carry no parameter name.
/// </remarks>
public static class TranscriptDrafts
{
    /// <summary>The draft of this conversation kept as this kind, over people's decided verdicts on it.</summary>
    public static TranscriptDraft Of(string id, string eval, string identity, string run, ConversationKey conversation,
        IReadOnlyList<HumanVerdict> verdicts, TranscriptKind kind, string? reason, IReadOnlyList<string> lines,
        string author, DateTimeOffset at)
    {
        if (verdicts.Count == 0)
            throw new ArgumentException(MessageText.Format(DraftMessage.NotDecided,
                new
                {
                    conversation.Mode,
                    conversation.Repeat,
                    conversation.Task
                }));
        bool failed = verdicts.Contains(HumanVerdict.Fail);
        if (kind == TranscriptKind.Reference && failed)
            throw new ArgumentException(MessageText.Format(DraftMessage.ReferenceNeedsPasses,
                new { conversation.Task }));
        if (kind == TranscriptKind.Bad && !failed)
            throw new ArgumentException(MessageText.Format(DraftMessage.KnownBadNeedsAFail, new { conversation.Task }));
        if (kind == TranscriptKind.Bad && reason is not null && !KebabCase.Is(reason))
            throw new ArgumentException(MessageText.Format(DraftMessage.ReasonNotKebabCase, new { Reason = reason }));

        // A reference keeps no reason; the file name refuses a known-bad without one.
        string? kept = kind == TranscriptKind.Bad ? reason : null;
        return new TranscriptDraft(id, eval, identity, run, conversation, kind, kept,
            TranscriptDraft.FileNameOf(conversation.Task, kind, kept), lines, author, at);
    }
}
