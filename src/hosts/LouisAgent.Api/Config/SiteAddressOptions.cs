namespace LouisAgent.Api.Config;

/// <summary>
/// Where a site lives, from SITE_ADDRESS (FX216): every page names it as the original's host, and a page asked for on
/// any other host is sent there. Without it, a site answers on whatever address it was asked at, as a developer's
/// localhost needs.
/// </summary>
/// <param name="Address">The site's address, ending in a slash; none when the setting is unset or not a site's.</param>
/// <remarks>
/// A plain options record with a reader that takes the variable lookup, as <see cref="OpenDoorOptions"/> is, so a test
/// or a host's configuration can hand it the setting. Only a whole https address with no path counts: a typo would
/// otherwise send every visitor somewhere odd, so anything else is taken as no setting.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0109-a-page-is-served-as-html.md">ADR 0109: a page is served as HTML</seealso>
public sealed record SiteAddressOptions(Uri? Address)
{
    /// <summary>The setting that names the site's address.</summary>
    public const string Setting = "SITE_ADDRESS";

    /// <summary>No address: the site answers on whatever address it was asked at.</summary>
    public static SiteAddressOptions None { get; } = new((Uri?)null);

    /// <summary>Reads the setting; the variable lookup is replaceable for tests and a host's configuration.</summary>
    public static SiteAddressOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string setting = (getVariable(Setting) ?? "").Trim();

        // A site's address is https, with nothing after the host but a slash.
        if (!Uri.TryCreate(setting, UriKind.Absolute, out var address)) return None;
        if (address.Scheme != Uri.UriSchemeHttps || address.AbsolutePath != "/") return None;
        return new SiteAddressOptions(new Uri(address.GetLeftPart(UriPartial.Authority) + "/"));
    }
}
