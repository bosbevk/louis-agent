using System.Text.Json.Serialization;

namespace LouisAgent.Web.Chats;

/// <summary>
/// The page the visitor is reading when they send a message (F115): its address, its title and its visible text, so
/// the guide can answer about what they see, and its areas (F117), so the guide can send them to one. The widget reads
/// it when a message is sent; the runner sends it with that message and forgets it. Every message carries the
/// section on the visitor's screen (F118) and the card open there, if one is (FX106); a later one from the same page
/// carries only those and the address. Its links (FX120) go with its words, each the address it opens and its text,
/// so the guide can take the visitor where a link on their screen leads. Its focus (F127.7) is the part of the site
/// it is in, as the site names it, so the guide reads that part's rules; the piece the visitor selected (F127.7) goes
/// with every message while it is selected.
/// </summary>
public sealed record Page(
    string Url,
    string Title,
    string Text,
    IReadOnlyList<PageArea> Areas,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    PageArea? InView = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    PageArea? Open = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<PageArea>? Links = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Focus = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    PageArea? Selected = null);
