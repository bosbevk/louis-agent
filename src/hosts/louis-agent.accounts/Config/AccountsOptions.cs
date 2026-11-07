namespace LouisAgent.Accounts.Config;

/// <summary>
/// The accounts' settings, read once at start-up the way <c>AgentOptions.FromEnvironment</c> reads the agent's: a
/// blank value counts as no value, and every value is trimmed.
/// </summary>
/// <param name="SqlUrl">
/// The accounts database's connection string (<c>ACCOUNTS_SQL_URL</c>, in <c>.env.secrets</c>); without it there
/// are no accounts and no account routes.
/// </param>
/// <param name="DefaultTenant">
/// The tenant a request lands in when no tenant row names its host (<c>ACCOUNTS_TENANT</c>, else <c>site</c>); its
/// row is made at start-up if it's missing.
/// </param>
/// <param name="CookieDomain">
/// The domain the sign-in cookie is set for (<c>ACCOUNTS_COOKIE_DOMAIN</c>); unset, the request's own host.
/// </param>
public sealed record AccountsOptions(string? SqlUrl, string DefaultTenant, string? CookieDomain)
{
    /// <summary>The tenant every account is in until a host picks another.</summary>
    public const string DefaultTenantId = "site";

    /// <summary>Whether this instance has accounts at all.</summary>
    public bool Enabled => SqlUrl is not null;

    /// <summary>Reads the settings from the environment, or from the lookup a test hands in, filling in the defaults.</summary>
    public static AccountsOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;

        // A blank setting is the same as a missing one; anything else comes back trimmed.
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();

        return new AccountsOptions(
            Get("ACCOUNTS_SQL_URL"),
            Get("ACCOUNTS_TENANT")?.ToLowerInvariant() ?? DefaultTenantId,
            Get("ACCOUNTS_COOKIE_DOMAIN"));
    }
}
