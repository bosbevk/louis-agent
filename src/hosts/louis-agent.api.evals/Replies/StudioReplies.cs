using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Api.Evals.Drafts.Composed;
using LouisAgent.Api.Evals.Studio;
using LouisAgent.Choices;

namespace LouisAgent.Api.Evals.Replies;

/// <summary>Builds the studio's catalogue reply from the runner's lines, each kind by its wire name.</summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as C# 14 static extension members, for the reason
/// <c>AdminReplies</c> gives: the records live in the contract, which can't see the runner's lines.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class StudioReplies
{
    /// <summary>The catalogue's reply.</summary>
    extension(CheckListReply)
    {
        /// <summary>The reply for what the runner listed: its checks, then its readers.</summary>
        public static CheckListReply Of(CheckListingResult listed) =>
            new([.. listed.Checks.Select(check => new CheckTypeRow(check.Type, check.Kind.Wire(), check.Parameters))],
                [.. listed.Readers.Select(reader => new ReaderRow(reader.Reader, reader.Kind.Wire()))]);
    }

    /// <summary>An eval draft's row.</summary>
    extension(EvalDraftRow)
    {
        /// <summary>The row for an eval being composed.</summary>
        public static EvalDraftRow Of(EvalDraft draft) =>
            new(draft.Id, draft.Identity, draft.Name, draft.Json,
                [.. draft.Rubrics.Select(r => new RubricRow(r.Name, r.Text))], draft.Author, draft.At,
                [.. draft.Scenarios.Select(s => new RubricRow(s.Name, s.Text))]);
    }

    /// <summary>The rubrics a request sent, as the draft keeps them.</summary>
    public static IReadOnlyList<RubricFile>? ToFiles(this IReadOnlyList<RubricRow>? rows) =>
        rows?.Select(r => new RubricFile(r.Name, r.Text ?? "")).ToList();
}
