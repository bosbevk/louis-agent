using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// The agent's widget in the corner of a mocked business's website (F127.1): where its customers would meet it, under
/// a head in the business's own brand. A visitor can fold it to its head to see the whole website, or spread it over
/// the website to read the conversation (F127.2), as a chat window does.
/// </summary>
/// <remarks>
/// State, kept small: the size is one value of <see cref="MockWidgetSize"/>, so the widget is never minimised and
/// maximised at once, and each button only flips between its own size and the corner. The words come from the site's
/// <see cref="ShowroomText"/>, so a site can say them in its own language.
/// </remarks>
public partial class MockWidget
{
    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The business's mark, before the label; none for a business without one.</summary>
    [Parameter]
    public RenderFragment? Mark { get; set; }

    /// <summary>The widget's title: "The Bakery · ask us".</summary>
    [Parameter]
    public string Label { get; set; } = "";

    /// <summary>The agent: its recorded chat, or a live one.</summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = null!;

    /// <summary>How big the chat sits now; in its corner until the visitor says otherwise.</summary>
    private MockWidgetSize Size { get; set; } = MockWidgetSize.Normal;

    /// <summary>The site's words, or the English ones.</summary>
    private ShowroomText Text => Services.GetService<ShowroomText>() ?? new ShowroomText();

    /// <summary>The first button folds the chat, or brings a folded one back.</summary>
    private string MinimiseLabel => Size == MockWidgetSize.Minimised ? Text.Restore : Text.Minimise;

    /// <summary>The second button spreads the chat over the website, or brings a spread one back.</summary>
    private string MaximiseLabel => Size == MockWidgetSize.Maximised ? Text.Restore : Text.Maximise;

    /// <summary>The widget's classes: its size, as the styles read it.</summary>
    private string Classes => Size switch
    {
        MockWidgetSize.Minimised => "mock-widget is-min",
        MockWidgetSize.Maximised => "mock-widget is-max",
        _ => "mock-widget",
    };

    /// <summary>Whether a button's size is the one the chat is in, for a screen reader.</summary>
    private string Pressed(MockWidgetSize size) => Size == size ? "true" : "false";

    /// <summary>Folds the chat to its head, or brings it back to its corner.</summary>
    private void ToggleMinimised() =>
        Size = Size == MockWidgetSize.Minimised ? MockWidgetSize.Normal : MockWidgetSize.Minimised;

    /// <summary>Spreads the chat over the website, or brings it back to its corner.</summary>
    private void ToggleMaximised() =>
        Size = Size == MockWidgetSize.Maximised ? MockWidgetSize.Normal : MockWidgetSize.Maximised;
}
