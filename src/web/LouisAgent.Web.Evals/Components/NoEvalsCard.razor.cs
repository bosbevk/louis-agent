using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components;

/// <summary>
/// The one card a host with no eval shows: where an eval file goes, and the guide, linked when the site names its
/// docs and named in words when it doesn't.
/// </summary>
public partial class NoEvalsCard
{
    /// <summary>The guide's address in the site's docs, or none when the site names no docs.</summary>
    [Parameter]
    public string? Guide { get; set; }
}
