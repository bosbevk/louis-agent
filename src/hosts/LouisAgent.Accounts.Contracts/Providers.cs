namespace LouisAgent.Accounts.Contracts;

/// <summary>
/// A way to sign in through someone else (Google, Microsoft, LinkedIn): its wire name, the label a button shows, and
/// whether this instance has its keys, since a button shows only for a provider that is set up.
/// </summary>
public sealed record ProviderInfo(string Name, string Label, bool Enabled);

/// <summary>What <c>GET /account/providers</c> answers.</summary>
public sealed record ProvidersReply(IReadOnlyList<ProviderInfo> Providers);
