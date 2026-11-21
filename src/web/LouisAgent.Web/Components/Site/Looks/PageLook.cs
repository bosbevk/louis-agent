namespace LouisAgent.Web.Components.Site.Looks;

/// <summary>
/// The look a page is in (F151.3), as the switch's module reads it off the page's root element: the theme's id and
/// the style's word, or none where the page says none.
/// </summary>
/// <param name="Theme">The theme's id, such as <c>glass</c>.</param>
/// <param name="Style">The style's word, <c>light</c> or <c>dark</c>.</param>
public sealed record PageLook(string? Theme, string? Style);
