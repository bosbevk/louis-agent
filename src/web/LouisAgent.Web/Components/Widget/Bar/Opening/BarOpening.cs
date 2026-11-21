namespace LouisAgent.Web.Components.Widget.Bar.Opening;

/// <summary>
/// How a site's agent bar opens (FX172, Louis: "site picks it"): the site names it through its areas, as it names its
/// guide's suggestions, and the bar reads it. A site that names none opens on a press.
/// </summary>
/// <param name="Opens">On a press, or under the pointer.</param>
public sealed record BarOpening(BarOpens Opens);
