using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LouisAgent.Web.Components.Editing.Inline;

/// <summary>
/// Words that are changed where they stand (F142). Click them and a box takes their place, with a tick and a cross:
/// Enter or the tick keeps what was typed, Escape or the cross drops it. With nothing listening for the change it
/// draws the plain words and nothing else, so the same page is a public page and, in an admin, its own editor.
/// </summary>
/// <remarks>
/// Inline edit, as the design systems call it (Atlassian, PatternFly): a read view and an edit view of one value,
/// with the change confirmed. Louis's pick of three, over a box dressed as the text (not quite the page's look) and
/// the text itself made editable (a script, and a fight with Blazor over the same words). Without it every page
/// someone may edit needs a form of its own beside it, and the two drift apart.
/// </remarks>
/// <seealso href="https://atlassian.design/components/inline-edit">Inline edit (Atlassian Design System)</seealso>
public partial class InlineEdit
{
    private bool _editing, _focus;
    private string _draft = "";
    private ElementReference _box;

    /// <summary>The words; nothing when there are none.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Told the words that were kept. With nothing listening, the words can't be changed.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>What the words are, for a screen reader and for where there are none yet: Name, Summary.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>Whether the words may run over several lines.</summary>
    [Parameter]
    public bool Multiline { get; set; }

    /// <summary>How many lines the box of a longer text shows.</summary>
    [Parameter]
    public int Rows { get; set; } = 5;

    /// <summary>Whether there are words to show.</summary>
    private bool HasValue => !string.IsNullOrWhiteSpace(Value);

    /// <summary>The cursor goes into the box the moment it is drawn.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focus) return;
        _focus = false;
        await _box.FocusAsync();
    }

    /// <summary>Swaps the words for a box that holds them.</summary>
    private void Open()
    {
        _draft = Value ?? "";
        _editing = true;
        _focus = true;
    }

    /// <summary>The words are a button, so Enter and the space bar open them too.</summary>
    private void OpenByKey(KeyboardEventArgs key)
    {
        if (key.Key is "Enter" or " ") Open();
    }

    /// <summary>What is in the box so far.</summary>
    private void Typed(ChangeEventArgs typed) => _draft = typed.Value?.ToString() ?? "";

    /// <summary>Escape drops. Enter keeps, but in a longer text it is a new line, and Ctrl with Enter keeps.</summary>
    /// <remarks>A switch on the key, one arm per outcome (ADR 0051).</remarks>
    private Task KeyAsync(KeyboardEventArgs key) => key switch
    {
        { Key: "Escape" } => DropAsync(),
        { Key: "Enter" } when !Multiline || key.CtrlKey || key.MetaKey => KeepAsync(),
        _ => Task.CompletedTask
    };

    /// <summary>Keeps what was typed, tidied. Words left as they were are no change, so nothing is told.</summary>
    private Task KeepAsync()
    {
        _editing = false;
        string typed = _draft.Trim();
        string? kept = typed.Length == 0 ? null : typed;
        return kept == Value ? Task.CompletedTask : ValueChanged.InvokeAsync(kept);
    }

    /// <summary>Drops what was typed.</summary>
    private Task DropAsync()
    {
        _editing = false;
        return Task.CompletedTask;
    }
}
