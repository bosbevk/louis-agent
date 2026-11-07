namespace LouisAgent.Web.Admin.Evals.List;

/// <summary>One mode's pass@k on the eval's last run, and how far it moved from the mode's run before.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record ModeChange(string Mode, int? K, double? PassAtK, double? Change);
