namespace LouisAgent.Api.Site.Rendering;

/// <summary>
/// A site's page rendered to HTML on the server (F121): its title, its description, its structured data (F123) and its
/// body.
/// </summary>
/// <param name="Title">The page's own title, from its <c>PageTitle</c>.</param>
/// <param name="Description">
/// What the page is about, from its description meta tag; empty when it declares none.
/// </param>
/// <param name="Body">The page's HTML, without its head.</param>
/// <param name="Schemas">Its JSON-LD scripts, each whole, from its <c>PageHead</c>; none when it has none.</param>
public sealed record RenderedPage(string Title, string Description, string Body, IReadOnlyList<string>? Schemas = null);
