namespace LouisAgent.Web.Components.Site.Addresses;

/// <summary>
/// Where an address points (F118, ADR 0107): the page, the section on it when it names one, and the page of a section
/// paged sideways when it names that too.
/// </summary>
/// <param name="Page">The page's own address, such as <c>/proof</c>, or <c>/</c> for the landing.</param>
/// <param name="Section">The section's id, such as <c>rand</c>; none for the page itself.</param>
/// <param name="Turn">The page of a section paged sideways, from one up; none for its first.</param>
public sealed record SiteAddress(string Page, string? Section = null, int? Turn = null);
