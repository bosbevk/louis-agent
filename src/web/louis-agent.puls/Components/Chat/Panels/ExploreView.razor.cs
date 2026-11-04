using LouisAgent.Puls.Content;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Chat.Panels;

public partial class ExploreView
{
    [Parameter] public EventCallback<Template> OnStart { get; set; }

    private static string Anchor(string section) => section.ToLowerInvariant();
}
