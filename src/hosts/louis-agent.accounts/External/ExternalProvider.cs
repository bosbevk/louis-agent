using LouisAgent.Choices;

namespace LouisAgent.Accounts.External;

/// <summary>
/// The providers a person may sign in through (F61.3). The wire name is the authentication scheme, the name Identity
/// keeps on the linked login, and the segment of <c>/account/external/{provider}</c>, so one name does for all three.
/// Facebook and X are named and not built: Facebook wants Meta's business verification, X closed its free tier.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum ExternalProvider
{
    [WireName("google")] Google,
    [WireName("microsoft")] Microsoft,
    [WireName("linkedin")] LinkedIn,
    [WireName("facebook")] Facebook,
    [WireName("x")] X,
}
