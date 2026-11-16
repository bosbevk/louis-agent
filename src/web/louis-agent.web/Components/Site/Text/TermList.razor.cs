using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Text;

/// <summary>
/// Lists named entries, such as a family's projects: each entry's names in code, then a sentence on what they are.
/// A site hands it the entries as records.
/// </summary>
/// <remarks>
/// One of the controls a site configures instead of writing HTML (ADR 0125, Louis: "all of ul and kids in a
/// control"): the list's look is the theme's, the same wherever a site lists its parts.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class TermList
{
    /// <summary>The entries, in the order they are listed.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<Term> Terms { get; set; } = [];
}
