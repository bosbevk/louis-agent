using LouisAgent.Puls.Admin.Explorer.Legends;
using LouisAgent.Puls.Admin.Explorer.Text;
using LouisAgent.Puls.Admin.Explorer.Timeline;
using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Parts;

/// <summary>Draws a turn's requests on a time axis, and opens one when you click its bar.</summary>
public partial class TurnTimeline
{
    /// <summary>The turn whose requests to draw.</summary>
    [Parameter, EditorRequired]
    public TurnNode Turn { get; set; } = null!;

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Opens a request; with none, the bars can't be clicked.</summary>
    [Parameter]
    public EventCallback<RequestNode> OnOpen { get; set; }

    /// <summary>The bars laid out on the axis; none for a turn with no request in the ledger.</summary>
    private TimelineLayout? Layout => Turn.Rows.Count == 0 ? null : TimelineLayout.Of(Turn);

    /// <summary>Puls's colour, or the specialist's.</summary>
    private static string Colour(TimelineLane lane) => lane.Specialist ? ExplorerColours.Spec : ExplorerColours.Own;

    /// <summary>A cost in the chosen currency, or a dash.</summary>
    private string Money(decimal? usd) => ExplorerText.Money(usd, Currency);
}
