using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Chat;

public partial class EmptyChat
{
    /// <summary>The message box, laid by the page: the same one it shows at the bottom once the chat has turns.</summary>
    [Parameter, EditorRequired] public RenderFragment Composer { get; set; } = default!;

    [Parameter] public bool Streaming { get; set; }

    /// <summary>A starter for the message box, for the user to finish and send.</summary>
    [Parameter] public EventCallback<string> OnDraft { get; set; }

    /// <summary>A suggestion's prompt, sent as it is.</summary>
    [Parameter] public EventCallback<string> OnSuggest { get; set; }

    // Topics under the empty chat's message box: each puts a starter in the box to finish and send.
    private static readonly (string Topic, string Starter)[] Topics =
    [
        ("Gesondheid", "Ek wil gesonder eet. Gee my 'n eenvoudige weekplan vir "),
        ("Leer", "Verduidelik vir my, eenvoudig en met 'n voorbeeld: "),
        ("Tegnologie", "Wat is die verskil tussen "),
        ("Lewe", "Help my om te besluit: "),
        ("Wetenskap", "Hoe werk "),
        ("Taal", "Help my om hierdie teks in beter Afrikaans te skryf: "),
    ];

    // Starters for an empty chat: each one shows a different kind of delegation.
    private static readonly (string Title, string Detail, string Prompt)[] Suggestions =
    [
        ("Soek aanlyn", "Wat is nuut in .NET 10?", "Wat is nuut in .NET 10? Gee my die hoogtepunte, met bronne."),
        ("Lees 'n lêer", "Som die README op", "Lees README.md en som in vyf punte op wat hierdie projek doen."),
        ("Kyk na die geskiedenis", "Wat het onlangs verander?", "Wat is die laaste vyf commits in hierdie repository, en wat het elkeen verander?"),
        ("Tel en reken", "Tel reëls kode", "Tel die reëls C#-kode onder louis-agent.core, per vouer, en wys die top vyf."),
    ];
}
