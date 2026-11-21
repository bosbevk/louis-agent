using AngleSharp.Dom;
using LouisAgent.Web.Components.Editing;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Tests.Admin.Forms;

/// <summary>Finds a form's box by the label over it, the way a person would.</summary>
internal static class Fields
{
    /// <summary>The box under this label.</summary>
    public static IElement Box(this IRenderedComponent<IComponent> within, string label) =>
        within.FindComponents<TextField>().Single(field => field.Instance.Label == label).Find(".field-box");

    /// <summary>The labels of every box, in the order they are drawn.</summary>
    public static IReadOnlyList<string> Labels(this IRenderedComponent<IComponent> within) =>
        [.. within.FindComponents<TextField>().Select(field => field.Instance.Label)];
}
