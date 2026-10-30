using LouisAgent.Choices;

namespace LouisAgent.Identities.Puls.Evals.Checks.Shared;

/// <summary>
/// The name the cook's eval is listed, run and stored under, for the checks and tests that need it. The eval's own file
/// says the same name; the routing eval's name lives only in its file.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum PulsEvalName
{
    [WireName("puls-cook")] Cook
}
