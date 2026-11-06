using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace LouisAgent.Web.Components.Chat.Composing;

/// <summary>The message box's toolbar: a file, the starters, the model and the voice.</summary>
public partial class ComposerTools
{
    [Inject] private ChatText Text { get; set; } = null!;

    /// <summary>The instance's model, prettified; null until known (the picker then says "Model").</summary>
    [Parameter] public string? Model { get; set; }
    [Parameter] public EventCallback<InputFileChangeEventArgs> OnFiles { get; set; }
    [Parameter] public EventCallback<string> OnDraft { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }
}
