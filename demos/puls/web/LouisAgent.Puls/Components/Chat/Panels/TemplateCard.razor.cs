using LouisAgent.Puls.Content;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Chat.Panels;

public partial class TemplateCard
{
    [Parameter, EditorRequired] public Template Template { get; set; } = default!;
    [Parameter] public EventCallback<Template> OnStart { get; set; }
}
