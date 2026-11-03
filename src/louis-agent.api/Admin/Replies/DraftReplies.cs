using System.Text.Json;
using LouisAgent.Api.Contracts.Admin.Drafts;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Evals.Drafts;
using LouisAgent.Api.Evals.Drafts.Samples;
using LouisAgent.Api.Evals.Drafts.Transcripts;
using LouisAgent.Choices;

namespace LouisAgent.Api.Admin.Replies;

/// <summary>
/// Builds the studio's rows from the drafts: a sample with the scenario file it will be, a transcript with its lines
/// as one text, an eval's drafts together, and the scenario a sample starts from.
/// </summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as C# 14 static extension members, for the reason
/// <c>AdminReplies</c> gives: the records live in the contract, which can't see the drafts.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class DraftReplies
{
    /// <summary>A sample draft's row.</summary>
    extension(SampleDraftRow)
    {
        /// <summary>The row for a sample draft, its scenario file written as the eval folders write theirs.</summary>
        public static SampleDraftRow Of(SampleDraft draft) =>
            new(draft.Id, draft.Eval, draft.Identity, draft.Run, ConversationKeyRow.Of(draft.Conversation),
                draft.Sample.Id, draft.Sample.Input[0].Content,
                JsonSerializer.Serialize(draft.Sample, SampleBody.FileJson),
                draft.Author, draft.At);
    }

    /// <summary>A transcript draft's row.</summary>
    extension(TranscriptDraftRow)
    {
        /// <summary>The row for a transcript draft, its lines joined as the file will hold them.</summary>
        public static TranscriptDraftRow Of(TranscriptDraft draft) =>
            new(draft.Id, draft.Eval, draft.Identity, draft.Run, ConversationKeyRow.Of(draft.Conversation),
                draft.Kind.Wire(), draft.Reason, draft.FileName, draft.Lines.Count,
                string.Concat(draft.Lines.Select(line => line + "\n")), draft.Author, draft.At);
    }

    /// <summary>An eval's drafts.</summary>
    extension(DraftsReply)
    {
        /// <summary>The reply for an eval's drafts, oldest first.</summary>
        public static DraftsReply Of(EvalDrafts drafts) =>
            new([.. drafts.Samples.Select(SampleDraftRow.Of)], [.. drafts.Transcripts.Select(TranscriptDraftRow.Of)]);
    }

    /// <summary>The scenario a sample starts from.</summary>
    extension(ScenarioRow)
    {
        /// <summary>The row for a scenario file: its id, place, time zone, expected values and categories.</summary>
        public static ScenarioRow Of(SampleBody scenario) =>
            new(scenario.Id, scenario.Metadata.Place, scenario.Metadata.TimeZone, scenario.Metadata.Expected,
                scenario.Metadata.Categories);
    }
}
