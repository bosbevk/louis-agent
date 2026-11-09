using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Meta;

/// <summary>
/// Says what a page is about (F121), beside Blazor's own <c>PageTitle</c>: one sentence or two, in the page's head,
/// which the browser shows and the framework's prerenderer serves to a search engine in the page's own HTML. A page
/// that declares one is a page meant for search; the framework prerenders only those.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class PageDescription
{
    /// <summary>
    /// What the page is about, as a search engine shows it under the title: about 50 to 170 characters.
    /// </summary>
    [Parameter, EditorRequired] public string Text { get; set; } = "";
}
