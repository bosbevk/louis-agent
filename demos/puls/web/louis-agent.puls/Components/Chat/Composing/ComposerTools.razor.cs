using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace LouisAgent.Puls.Components.Chat.Composing;

public partial class ComposerTools
{
    /// <summary>The instance's model, prettified; null until known (the picker then says "Model").</summary>
    [Parameter] public string? Model { get; set; }
    [Parameter] public EventCallback<InputFileChangeEventArgs> OnFiles { get; set; }
    [Parameter] public EventCallback<string> OnDraft { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }
}
