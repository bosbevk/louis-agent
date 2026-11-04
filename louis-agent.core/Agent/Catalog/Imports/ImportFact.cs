namespace LouisAgent.Core.Agent.Catalog.Imports;

/// <summary>One line of an import's report, a label and its value ("Products", "220"), written by the pack.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values</seealso>
public sealed record ImportFact(string Label, string Value);
