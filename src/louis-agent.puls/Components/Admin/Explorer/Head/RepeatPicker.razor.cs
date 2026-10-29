using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Head;

/// <summary>Picks which repeat of the run the explorer shows.</summary>
public partial class RepeatPicker
{
    /// <summary>How many repeats the run has.</summary>
    [Parameter, EditorRequired]
    public int Repeats { get; set; }

    /// <summary>The repeat shown now.</summary>
    [Parameter, EditorRequired]
    public int Repeat { get; set; }

    /// <summary>Someone picked a repeat.</summary>
    [Parameter]
    public EventCallback<int> OnPick { get; set; }
}
