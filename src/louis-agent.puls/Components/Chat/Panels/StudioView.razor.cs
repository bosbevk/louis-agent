using LouisAgent.Puls.Content;
using Microsoft.AspNetCore.Components;
using ChatModel = LouisAgent.Web.Shared.Chats.Chat;

namespace LouisAgent.Puls.Components.Chat.Panels;

public partial class StudioView
{
    [Parameter, EditorRequired] public IReadOnlyList<ChatModel> Recent { get; set; } = [];
    [Parameter] public EventCallback<Template> OnStart { get; set; }
    [Parameter] public EventCallback OnNewProject { get; set; }

    private string? _category;

    /// <summary>"5 minute gelede", "gister", "3 dae gelede": when a chat was last used.</summary>
    private static string Ago(DateTimeOffset when)
    {
        var age = DateTimeOffset.Now - when;
        return age.TotalMinutes < 1 ? "Nou net"
            : age.TotalHours < 1 ? $"{(int)age.TotalMinutes} minute gelede"
            : age.TotalDays < 1 ? $"{(int)age.TotalHours} uur gelede"
            : age.TotalDays < 2 ? "Gister"
            : $"{(int)age.TotalDays} dae gelede";
    }
}
