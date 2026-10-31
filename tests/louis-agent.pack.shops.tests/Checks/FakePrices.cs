using LouisAgent.Evals.Readers;

namespace LouisAgent.Packs.Shops.Tests.Checks;

/// <summary>
/// A price reader that says what it's told, whatever the answer: how many rows give a price as fact, and which sites
/// the prices link to. The shop rules' tests use it, since how an answer writes a price is an identity's reader's to
/// know.
/// </summary>
/// <remarks>A Fake (Meszaros): the real readers are tested beside the identities that ship them.</remarks>
internal sealed class FakePrices(int asFact = 0, params string[] linkedSites) : IPriceReader
{
    /// <summary>A reader that finds no price given as fact.</summary>
    public static FakePrices None { get; } = new();

    /// <inheritdoc />
    public string Name => "fake-prices";

    /// <inheritdoc />
    public int AsFact(string answer) => asFact;

    /// <inheritdoc />
    public IReadOnlyList<string> LinkedSites(string answer) => linkedSites;
}
