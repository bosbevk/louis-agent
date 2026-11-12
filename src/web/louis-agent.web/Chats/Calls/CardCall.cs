using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tickets;

namespace LouisAgent.Web.Chats.Calls;

/// <summary>
/// The card a ShowCard call draws (F127.4.1): the agent wrote a ticket's parts, and the diagrams library reads them
/// back into the ticket it draws; any other call, a failed one, or one the library wouldn't draw, is no card.
/// </summary>
/// <remarks>
/// The same shape as <see cref="ShowPageCall"/>: the web names the tool and never the pack, which it can't reference
/// (ADR 0027), and the call's reading is the library's own (<c>TicketCard.FromCall</c>), so the pack that said
/// "shown" and the page that draws it read a call one way.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0105-the-guide-acts-on-the-page-through-a-tool-the-widget-hears.md">ADR 0105: the guide acts on the page through a tool the widget hears</seealso>
public static class CardCall
{
    /// <summary>The site pack's tool that shows the visitor a card.</summary>
    public const string Tool = "ShowCard";

    /// <summary>The ticket a ShowCard call draws; none for any other call.</summary>
    public static DiagramSource? SourceOf(Block call) =>
        call.ToolName != Tool || call.ToolFailed ? null : TicketCard.FromCall(call.ToolInput);
}
