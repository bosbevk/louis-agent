using LouisAgent.Evals.Readers;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;

namespace LouisAgent.Identities.Puls.Evals.Readers;

/// <summary>
/// Reads how many people a meal is for, in digits or in words ("vir vier mense", "a stew for two"), in Afrikaans or
/// English, leaving out numbers that are amounts of something.
/// </summary>
/// <remarks>
/// An Adapter (Gang of Four, Louis's pick) over <see cref="Servings.Of"/>, the cook's serving rule's own reading, so
/// <c>number-named</c> over this reader grades as <c>serves-as-asked</c> did. The eval file names it
/// <c>servings</c>; the class is <c>PeopleServed</c> because <see cref="Servings"/> is the reading it wraps.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class PeopleServed : INumberReader
{
    /// <inheritdoc />
    public string Name => "servings";

    /// <inheritdoc />
    public IReadOnlyList<int> Numbers(string answer) => Servings.Of(answer);
}
