namespace LouisAgent.Web.Components.Areas.Focuses;

/// <summary>One part of a site and its focus: every page at or under an address, such as <c>/framework</c>.</summary>
/// <param name="Under">The part's address, from the site's root.</param>
/// <param name="Focus">The focus its pages take, by the name the site's agent knows it by.</param>
public sealed record FocusPart(string Under, string Focus);
