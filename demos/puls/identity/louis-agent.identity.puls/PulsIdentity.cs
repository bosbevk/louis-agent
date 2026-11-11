using LouisAgent.Composition.Packs;
using LouisAgent.Choices;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Identities.Puls;

/// <summary>
/// Puls, the South African general assistant (CL15, ADR 0027): its modes (<c>agent/puls/</c>), its specialists
/// (<c>specialists/puls-*</c>) and its minion roles (<c>minions/puls-*</c>), and the packs it uses by name. Loaded by a
/// host with <c>IDENTITIES=puls</c> (the Puls compose sets it in both agent containers); the code-review stack never
/// loads it.
/// </summary>
public sealed class PulsIdentity : IAgentIdentity
{
    /// <summary>The name the host loads Puls by, <c>puls</c>.</summary>
    public string Name => PulsIdentityName.Puls.Wire();

    /// <summary>
    /// Puls's prompts, embedded with their paths (<c>agent/puls/voice.md</c>, <c>specialists/puls-cook.md</c>), read
    /// only from this assembly.
    /// </summary>
    /// <inheritdoc/>
    public string DefaultVoice => $"{Name}/voice-af";

    public IFileProvider Prompts { get; } = EmbeddedPrompts.Of(typeof(PulsIdentity).Assembly);

    /// <summary>
    /// Where to buy and shop prices (F50.2) and the recipes (F35); CL16 adds <c>cooking</c>, later <c>places</c>.
    /// </summary>
    public IReadOnlyList<string> Packs =>
    [
        PulsPack.WhereToBuy.Wire(), PulsPack.ShopPrices.Wire(), PulsPack.Recipes.Wire(), PulsPack.Web.Wire(),
        PulsPack.Calculator.Wire()
    ];

    /// <summary>
    /// Its five specialists, by address (F127.3.1); which of them is on is still the instance's <c>ROLES_ENABLED</c>.
    /// </summary>
    public IReadOnlyList<string> Calls => [.. Enum.GetValues<PulsCall>().Select(call => call.Wire())];

    /// <summary>
    /// A specialist's report goes to the user as it is (ADR 0079), so Puls's model never writes the cook's recipe out
    /// again.
    /// </summary>
    public bool RelaysAnswersWhole => true;
}

/// <summary>The specialists Puls may ask, by their addresses (ADR 0043: an identity keeps its own enum).</summary>
public enum PulsCall
{
    [WireName("puls.cook")] Cook,
    [WireName("puls.researcher")] Researcher,
    [WireName("puls.analyst")] Analyst,
    [WireName("puls.historian")] Historian,
    [WireName("puls.reader")] Reader
}

/// <summary>
/// The name this identity is loaded by (<c>IDENTITIES=puls</c>) and that the evals prove (ADR 0043). It is written
/// once, here; the ratchet bans the literal everywhere outside an enum, so the one definition is a member too.
/// </summary>
public enum PulsIdentityName
{
    [WireName("puls")] Puls,
}
