namespace LouisAgent.Composition;

using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Messages;
using LouisAgent.Identities.Puls;
using LouisAgent.Packs.Shops;

/// <summary>
/// The composition root (CL15, ADR 0027): the one place that knows every identity and pack. A host calls
/// <see cref="FromEnvironment"/> once at start-up and hands the catalogue to <c>AgentHost.Build</c>; the catalogue then
/// checks that what was named fits together. Synchronous in CL15: nothing to await yet (F36's MCP packs connect here).
/// </summary>
public static class Plugins
{
    /// <summary>
    /// <c>IDENTITIES</c> and <c>PACKS</c> from the environment, after <c>config/.env</c> is loaded: the hosts load it
    /// inside <c>AgentHost.Build</c>, which runs after this, so a setting made only there would otherwise be read too
    /// late. Loading twice is harmless (a variable already set wins).
    /// </summary>
    public static AgentCatalog FromEnvironment()
    {
        AgentHost.LoadEnvironment();
        return FromSettings(Environment.GetEnvironmentVariable("IDENTITIES"), Environment.GetEnvironmentVariable("PACKS"));
    }

    /// <summary>
    /// <c>IDENTITIES=puls</c> and <c>PACKS=shops</c>, comma separated, case ignored, each name once: every identity with the
    /// packs it names, plus the packs named on their own; one instance per pack, however often it is named. An unknown
    /// name is refused before anything is built; unset loads nothing.
    /// </summary>
    public static AgentCatalog FromSettings(string? identities, string? packs)
    {
        var identityKinds = Parse<IdentityKind>(identities, CompositionErrorMessage.UnknownIdentity);
        var packKinds = Parse<PackKind>(packs, CompositionErrorMessage.UnknownPack);

        var chosen = identityKinds.Select(Create).ToList();
        var named = packKinds
            .Concat(chosen.SelectMany(i => i.Packs).Select(name => SettingChoice.TryParse(name, out PackKind? kind) ? kind : null).OfType<PackKind>())
            .Distinct()
            .ToList();
        // A name no pack has is left to the catalogue, which refuses it naming the registered ones.
        return new AgentCatalog(chosen, named.Select(Create).ToList());
    }

    private static IAgentIdentity Create(IdentityKind kind) => kind switch
    {
        IdentityKind.Puls => new PulsIdentity(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static IAgentPack Create(PackKind kind) => kind switch
    {
        PackKind.Shops => new ShopsPack(ShopsOptions.FromEnvironment()),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static IReadOnlyList<T> Parse<T>(string? setting, CompositionErrorMessage unknown) where T : struct, Enum
    {
        var kinds = new List<T>();
        foreach (string name in (setting ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!SettingChoice.TryParse(name, out T? kind) || kind is null)
                throw new AgentCatalogException(MessageText.Format(unknown, new
                {
                    Name = name, Supported = SettingChoice.Names<T>() is { Count: > 0 } names ? string.Join(", ", names) : "none",
                }));
            if (!kinds.Contains(kind.Value)) kinds.Add(kind.Value);
        }
        return kinds;
    }
}
