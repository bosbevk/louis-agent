using LouisAgent.Choices;

namespace LouisAgent.Identities.Puls.Evals.Checks.Cook;

/// <summary>
/// The two ways the cook's eval runs the same conversations: Puls with its own shop tools (<c>puls-tools</c>), or Puls
/// handing the meal to its cook (<c>puls-cook</c>). It's a run's label, not a Puls mode file and not a role.
/// </summary>
/// <remarks>
/// The checks keep their own enum of the names they use rather than borrowing another project's (ADR 0043).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum CookMode
{
    [WireName("puls-tools")] Tools,
    [WireName("puls-cook")] Cook,
}
