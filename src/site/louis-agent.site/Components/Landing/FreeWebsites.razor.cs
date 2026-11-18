using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Site.Components.Landing;

/// <summary>
/// The band that offers two websites for free, to build a portfolio. Businesses enter, and two are chosen.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class FreeWebsites
{
    /// <summary>How it goes, from an entry to a website that is live.</summary>
    private static readonly IReadOnlyList<Step> HowItGoes =
    [
        new("Send us your details.", "Orbi asks what your business does and how to reach you. It takes two minutes."),
        new("Then we choose two.", "We read every entry, choose two businesses and call them."),
        new("We build it with you.", "We agree what your agent answers, and you see it work before it goes live.")
    ];
}
