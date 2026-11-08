using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Widget.Door;

/// <summary>
/// A button anywhere on a site that asks the guide a question (F118, Louis: "a quick entry point to the walkthrough",
/// "one you cannot miss"): "Take the tour" on a landing opens the guide and starts it walking the visitor through the
/// site. The words and the question are the site's.
/// </summary>
public partial class GuideButton
{
    /// <summary>What the button asks the guide.</summary>
    [Parameter, EditorRequired] public string Question { get; set; } = "";

    /// <summary>The button's words.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The site's own classes, after the button's.</summary>
    [Parameter] public string? Class { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <summary>Puts the question through the door, when the site has the guide.</summary>
    private void Ask() => Services.GetService<GuideDoor>()?.Ask(Question);
}
