using LouisAgent.Evals.Catalog;

namespace LouisAgent.Identities.Puls.Judges;

/// <summary>
/// Puls's own model judges that an eval file can name: today the cook's <c>meal-for-people</c>. The runner gathers
/// this list with the general checks and Puls's fixed rules.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA), split from <c>PulsChecks</c> by what each needs (the Stable Dependencies Principle): a
/// judge is built on the harness's judge, which the identity must never reference, so the judges live in this
/// project, which only the runner references, while the fixed rules stay beside the identity.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class PulsJudges
{
    /// <summary>Every judge of Puls's an eval file can name.</summary>
    public static IReadOnlyList<ICheckType> All { get; } = [new MealForPeopleCheck.Entry()];
}
