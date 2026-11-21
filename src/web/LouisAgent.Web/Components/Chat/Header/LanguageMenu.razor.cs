using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat.Header;

/// <summary>Which language a new chat speaks, from those the API has a voice for.</summary>
public partial class LanguageMenu
{
    [Inject] private LouisAgent.Web.Text.ChatText Text { get; set; } = null!;

    [Parameter, EditorRequired] public string Language { get; set; } = "en";
    [Parameter, EditorRequired] public IReadOnlyList<string> Languages { get; set; } = ["en"];
    [Parameter] public EventCallback<string> OnChoose { get; set; }

    /// <summary>A language's name in the menu; a code without one is shown as it is.</summary>
    private static string LanguageName(string code) => code switch { "af" => "Afrikaans", "en" => "English", _ => code };
}
