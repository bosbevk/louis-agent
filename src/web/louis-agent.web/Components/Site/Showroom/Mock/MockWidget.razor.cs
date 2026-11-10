using LouisAgent.Web.Components.Widget;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// The agent's widget in the corner of a mocked business's website (F127.1): where its customers would meet it, under
/// a head in the business's own brand. A visitor can fold it to its head to see the whole website, or spread it over
/// the website to read the conversation (F127.2), as a chat window does.
/// </summary>
/// <remarks>
/// It keeps its size and draws itself from it; the buttons are the chat window's own (<see cref="WindowButtons"/>),
/// shared with the guide's widget, which keep the one-size-at-a-time rule.
/// </remarks>
public partial class MockWidget
{
    /// <summary>The business's mark, before the label; none for a business without one.</summary>
    [Parameter]
    public RenderFragment? Mark { get; set; }

    /// <summary>The widget's title: "The Bakery · ask us".</summary>
    [Parameter]
    public string Label { get; set; } = "";

    /// <summary>Whether a live agent answers in it, rather than a recording (F127.2).</summary>
    [Parameter]
    public bool Live { get; set; }

    /// <summary>The agent: its recorded chat, or a live one.</summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = null!;

    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The showroom's words, the site's own when it has them.</summary>
    private ShowroomText Text => Services.GetService<ShowroomText>() ?? new ShowroomText();

    /// <summary>How big the chat sits now; in its corner until the visitor says otherwise.</summary>
    private ChatWindowSize Size { get; set; } = ChatWindowSize.Normal;

    /// <summary>The widget's classes: its size, as the styles read it.</summary>
    private string Classes => Size switch
    {
        ChatWindowSize.Minimised => "mock-widget is-min",
        ChatWindowSize.Maximised => "mock-widget is-max",
        _ => "mock-widget",
    };
}
