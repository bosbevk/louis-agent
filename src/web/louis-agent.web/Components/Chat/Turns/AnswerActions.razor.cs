using LouisAgent.Web.Chats;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat.Turns;

/// <summary>Under a finished answer: copy, regenerate, good and bad, share, and its sources.</summary>
public partial class AnswerActions
{
    [Inject] private ChatText Text { get; set; } = null!;

    [Parameter, EditorRequired] public Turn Turn { get; set; } = default!;
    [Parameter] public EventCallback<string> OnCopy { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }

    /// <summary>The answer's own words: its text after the last tool call or thought.</summary>
    private static string AnswerText(Turn turn) => SpecialistAnswer.Split(turn).Answer.Trim();
}
