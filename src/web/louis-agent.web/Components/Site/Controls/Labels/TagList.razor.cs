using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Controls.Labels;

/// <summary>
/// A row of plain tags that wraps, such as the tools a job was built with. With no tags it draws nothing.
/// </summary>
/// <remarks>Composite, in Razor: the row lays out, and each <see cref="Tag"/> draws itself.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class TagList
{
    /// <summary>The tags' words, in order.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<string> Tags { get; set; } = [];
}
