namespace LouisAgent.Composition.Packs.Imports;

/// <summary>One file an import takes: its name in the upload ("provinces") and what to pick, for the admin page.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values</seealso>
public sealed record ImportSlot(string Name, string Description);
