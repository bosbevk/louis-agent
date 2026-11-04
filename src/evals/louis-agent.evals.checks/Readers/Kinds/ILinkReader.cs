namespace LouisAgent.Evals.Readers.Kinds;

/// <summary>
/// Reads the link to the recipe's page out of an answer: the one line where the cook says where the recipe came from.
/// </summary>
/// <remarks>
/// A Strategy of its own kind (Gang of Four, ADR 0059), as <see cref="IPriceReader"/> is: an identity's reader knows
/// how its agents write that line (the word before it, in its languages, and how the link is dressed), the pack's
/// check knows what makes a link good, so the pack never names an identity.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0059-an-identity-ships-readers-not-checks.md">ADR 0059: an identity ships readers, not checks</seealso>
public interface ILinkReader : IReader
{
    /// <summary>
    /// The address of the recipe's page, or null when the answer gives none (ADR 0007: none is null).
    /// </summary>
    string? RecipeLink(string answer);
}
