using LouisAgent.Web.Components.Site.Meta.Schema;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Meta;

/// <summary>
/// What a page is, in a search engine's own words (F123, Louis's pick of three: a component in the page's head): a
/// page places it beside <c>PageTitle</c> and <c>PageDescription</c> with one of the framework's schema.org records,
/// and it lands in the head as JSON-LD.
/// </summary>
/// <remarks>
/// The same shape as <c>PageDescription</c>: the head holds what a page says about itself, and the prerender (F121)
/// carries the head into the served HTML, so a crawler that runs no script reads it too.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0109-a-page-is-served-as-html.md">ADR 0109: a page is served as HTML</seealso>
public partial class PageSchema
{
    /// <summary>The record, one of the framework's schema.org records.</summary>
    [Parameter, EditorRequired] public object Value { get; set; } = default!;

    /// <summary>The whole script element, its JSON safe inside it.</summary>
    private string Script => $"<script type=\"application/ld+json\">{SchemaJson.Write(Value)}</script>";
}
