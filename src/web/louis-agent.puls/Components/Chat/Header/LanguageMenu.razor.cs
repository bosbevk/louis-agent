using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Chat.Header;

public partial class LanguageMenu
{
    [Parameter, EditorRequired] public string Language { get; set; } = "af";
    [Parameter, EditorRequired] public IReadOnlyList<string> Languages { get; set; } = ["af"];
    [Parameter] public EventCallback<string> OnChoose { get; set; }

    /// <summary>A language's name in the menu; a code without one is shown as it is.</summary>
    private static string LanguageName(string code) => code switch { "af" => "Afrikaans", "en" => "English", _ => code };
}
