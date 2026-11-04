using LouisAgent.Web.Shared.Chats;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Chat.Turns;

public partial class AnswerActions
{
    [Parameter, EditorRequired] public Turn Turn { get; set; } = default!;
    [Parameter] public EventCallback<string> OnCopy { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }

    /// <summary>The answer's own words: its text after the last tool call or thought (the reply the user reads), as the specialist card splits a turn.</summary>
    private static string AnswerText(Turn turn) => SpecialistAnswer.Split(turn).Answer.Trim();
}
