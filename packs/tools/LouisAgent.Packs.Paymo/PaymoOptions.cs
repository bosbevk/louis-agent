namespace LouisAgent.Packs.Paymo;

/// <summary>The Paymo pack's own setting.</summary>
/// <param name="ApiKey">PAYMO_API_KEY. Without it the toolset stays off.</param>
public sealed record PaymoOptions(string? ApiKey = null)
{
    /// <summary>The key from the environment, once the host has loaded its settings files.</summary>
    public static PaymoOptions FromEnvironment() => new(Environment.GetEnvironmentVariable("PAYMO_API_KEY"));
}
