using LouisAgent.Composition.Messages;
using LouisAgent.Web.Docs.Messages;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Docs.Links;

/// <summary>
/// A link to one doc by its path in the repo, such as <c>src/docs/adr/0126-….md</c>: the page says which doc, and
/// the site's docs' address says where it lives, so the docs can move without a page changing.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public partial class DocLink
{
    /// <summary>The doc's path in the repo.</summary>
    [Parameter, EditorRequired]
    public string Path { get; set; } = "";

    /// <summary>The link's words.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Where the site's docs live; none named is a site set up wrong, said at the first doc link.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    /// <summary>The doc's whole web address.</summary>
    private string Href => Address.For(Path);

    /// <summary>The site's docs' address, or the reason it has none and the cure.</summary>
    private DocsAddress Address => Services.GetService(typeof(DocsAddress)) as DocsAddress
                                   ?? throw new InvalidOperationException(
                                       MessageText.Format(DocsErrorMessage.NoDocsAddress));
}
