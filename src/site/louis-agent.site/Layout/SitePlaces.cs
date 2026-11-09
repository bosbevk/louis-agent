using LouisAgent.Web.Components.Site.Addresses;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Layout;

/// <summary>
/// A place's own address for this site's router (FX124): <c>/home/for-you</c> or <c>/framework/measured/rule</c>
/// opened from a link, a reload or a search draws the page it names, as a click inside the site always did.
/// </summary>
/// <remarks>
/// The framework's place page is routed from the framework's assembly, which a site with admin pages hands its router
/// whole. This site has no admin, so it takes the place page alone: a plain class that carries the route and inherits
/// the page, its drawing and all, rather than routing the framework's admin screens to visitors.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every section has an address</seealso>
[Route("/{*Path}")]
public sealed class SitePlaces : SectionPage;
