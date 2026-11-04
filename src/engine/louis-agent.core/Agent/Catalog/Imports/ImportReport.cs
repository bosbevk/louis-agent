namespace LouisAgent.Core.Agent.Catalog.Imports;

/// <summary>
/// What an import found in the files and what it did: the period the data covers ("January 2017 to September 2026",
/// <c>null</c> when the files couldn't be read), how many things saving writes (new ones and corrected ones; 0 means
/// it is all stored already), whether this call saved them, the facts a person checks before saving, and the problems
/// (a wrong file). Nothing is saved while there is a problem; a warning the pack can live with is a fact.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values</seealso>
public sealed record ImportReport(
    string? Period,
    int Changes,
    bool Saved,
    IReadOnlyList<ImportFact> Facts,
    IReadOnlyList<string> Problems);
