namespace LouisAgent.Web.Components.Site.Slides;

/// <summary>
/// How a site's page is laid out (F118, Louis: "like a layout option in the framework"): as slides, each section a
/// screen held in the middle with the page settling on one at a time, or as an ordinary page that flows.
/// </summary>
public enum PageLayout
{
    /// <summary>An ordinary page: sections as tall as their content, the page scrolling freely.</summary>
    Flow,

    /// <summary>Each section one screen, its content in the middle, the page settling on a section as it scrolls; a section with more than a screen pages sideways.</summary>
    Slides,

    /// <summary>Docs-type content (F164.1): the site's menu on the left, the page in a readable middle column, and the page's sections on the right as "On this page".</summary>
    Docs
}
