using LouisAgent.Web.Components.Site.Meta.Schema;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Meta;

/// <summary>
/// Says what a page is about and what it is (F121, F123), beside Blazor's own <c>PageTitle</c>: a sentence or two,
/// and any structured data the page declares, in the page's head, which the browser keeps and the framework's
/// prerenderer serves to a search engine in the page's own HTML. A page that declares a description is a page meant for
/// search; the framework prerenders only those.
/// </summary>
/// <remarks>
/// One component writes the whole of what a page says in its head, because Blazor keeps only the last
/// <c>HeadContent</c> a page renders: a second component writing there would push the first out (Louis's pick of three
/// on F123, over a description component that also takes schemas and over two components joined by a named section).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0109-a-page-is-served-as-html.md">ADR 0109: a page is served as HTML</seealso>
public partial class PageHead
{
    /// <summary>
    /// What the page is about, as a search engine shows it under the title: about 50 to 170 characters.
    /// </summary>
    [Parameter, EditorRequired]
    public string Description { get; set; } = "";

    /// <summary>What the page is in schema.org's words: the framework's schema records, or none.</summary>
    [Parameter]
    public IReadOnlyList<object>? Schemas { get; set; }

    /// <summary>Each schema as a whole JSON-LD script element, its JSON safe inside it.</summary>
    private IEnumerable<string> Scripts => (Schemas ?? []).Select(schema =>
        $"<script type=\"application/ld+json\">{SchemaJson.Write(schema)}</script>");
}
