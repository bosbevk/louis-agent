using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Editing;

/// <summary>
/// One labelled box of a form a person fills in (F142): a line, or a box of several lines. It hands back what is
/// typed as it is typed, so a preview beside the form keeps up, and nothing at all for a box left empty, so a field
/// that may be left out stays out.
/// </summary>
public partial class TextField
{
    /// <summary>What the box is for, shown over it.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>What it holds; nothing for an empty box.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>
    /// Told what the box holds now: on every keystroke, and once more, tidied, when the person leaves it.
    /// </summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Whether the box holds several lines.</summary>
    [Parameter]
    public bool Multiline { get; set; }

    /// <summary>How many lines a box of several shows before it scrolls.</summary>
    [Parameter]
    public int Rows { get; set; } = 3;

    /// <summary>A line of help under the box.</summary>
    [Parameter]
    public string? Hint { get; set; }

    /// <summary>What an empty box suggests.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// A keystroke: handed back as typed. Tidying it here would take away the space just typed between two words.
    /// </summary>
    private Task TypedAsync(ChangeEventArgs typed)
    {
        string text = typed.Value?.ToString() ?? "";
        return ValueChanged.InvokeAsync(string.IsNullOrWhiteSpace(text) ? null : text);
    }

    /// <summary>The person left the box: the spaces around what they typed go.</summary>
    private Task LeftAsync(ChangeEventArgs left)
    {
        string text = (left.Value?.ToString() ?? "").Trim();
        return ValueChanged.InvokeAsync(text.Length == 0 ? null : text);
    }
}
