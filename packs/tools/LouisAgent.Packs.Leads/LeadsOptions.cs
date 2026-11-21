namespace LouisAgent.Packs.Leads;

/// <summary>
/// The leads pack's settings, read once by the composition root: where a lead goes, who it comes from, and the key
/// that sends it for real. Without <c>LEADS_TO</c> the pack stays off; without the key the lead goes to the log.
/// </summary>
/// <remarks>
/// A plain options object with a reader that takes the variable lookup, as <c>DocsOptions</c> is, so a test needs no
/// real environment.
/// </remarks>
public sealed class LeadsOptions
{
    /// <summary>The setting that turns the toolset on: the owner's address.</summary>
    public const string ToSetting = "LEADS_TO";

    /// <summary>The verified sender's address; the default is the site's guide.</summary>
    public const string FromSetting = "LEADS_FROM";

    /// <summary>Resend's key, in the secrets. Unset, nothing is sent.</summary>
    public const string ApiKeySetting = "RESEND_API_KEY";

    /// <summary>LEADS_TO: where a lead goes.</summary>
    public string? To { get; init; }

    /// <summary>LEADS_FROM: who the mail comes from; a verified sender on the owner's domain.</summary>
    public string From { get; init; } = "guide@louis-agent.dev";

    /// <summary>RESEND_API_KEY: the key that sends for real.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Reads the settings; the variable lookup is replaceable for tests.</summary>
    public static LeadsOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? from = Clean(getVariable(FromSetting));
        return new LeadsOptions
        {
            To = Clean(getVariable(ToSetting)),
            From = from ?? new LeadsOptions().From,
            ApiKey = Clean(getVariable(ApiKeySetting))
        };
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
