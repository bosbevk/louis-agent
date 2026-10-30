using LouisAgent.Choices;

namespace LouisAgent.Identities.Puls.Evals.Checks.Shared;

/// <summary>
/// The roles Puls's checks name: the cook, the specialist a question about a meal goes to, and the role a turn that
/// started it reports. A member's wire name is the role's name in a session's stream.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum PulsEvalRole
{
    [WireName("puls-cook")] Cook,
}
