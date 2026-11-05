using LouisAgent.Choices;

namespace LouisAgent.Api.Evals.Drafts.Store.Documents;

/// <summary>
/// Which kind of draft a document in <c>eval_drafts</c> holds, so each kind's typed view of the one collection reads
/// only its own documents.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
internal enum DraftKind
{
    [WireName("sample")] Sample,
    [WireName("transcript")] Transcript,
    [WireName("eval")] Eval,
}
