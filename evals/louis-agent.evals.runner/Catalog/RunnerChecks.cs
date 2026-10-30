using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Puls.Evals.Cook.Grading;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// The check catalogue the runner gathers from every owner's list: the harness's general checks, then Puls's own.
/// An eval file can name any of them, and a type listed twice stops the runner's start.
/// </summary>
/// <remarks>
/// The runner is the one project that knows every evals project, as the composition is for identities and packs
/// (ADR 0027), so it is where the lists meet. Puls's moves beside the identity in F49.2 step 9, reached through the
/// composition.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class RunnerChecks
{
    /// <summary>Every check type the runner's evals can name.</summary>
    public static CheckCatalog Catalog { get; } = new([.. GeneralChecks.All, .. PulsChecks.All]);
}
