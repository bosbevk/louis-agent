using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat.Header;

/// <summary>The profile's menu: a guest for now, with credits, the theme, settings, admin and sign out.</summary>
public partial class ProfileMenu
{
    [Inject] private ChatText Text { get; set; } = null!;

    [Parameter] public EventCallback<string> OnComingSoon { get; set; }
}
