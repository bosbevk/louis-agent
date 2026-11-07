namespace LouisAgent.Accounts.External;

/// <summary>
/// What a provider's app registration gives us: the client id and its secret. A provider is on only when both are
/// set, the toolsets' rule (<c>?</c> only where the instance has it).
/// </summary>
public sealed record ProviderKeys(string? ClientId, string? ClientSecret)
{
    /// <summary>Nothing set: the provider stays off.</summary>
    public static readonly ProviderKeys None = new(null, null);

    /// <summary>Whether the instance has what the provider needs.</summary>
    public bool IsSet => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
