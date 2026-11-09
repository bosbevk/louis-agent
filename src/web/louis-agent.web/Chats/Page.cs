using System.Text.Json.Serialization;

namespace LouisAgent.Web.Chats;

/// <summary>
/// The page the visitor is reading when they send a message (F115): its address, its title and its visible text, so
/// the guide can answer about what they see, and its areas (F117), so the guide can send them to one. The widget reads
/// it when a message is sent; the runner sends it with that message and forgets it. Every message carries the
/// section on the visitor's screen (F118) and the card open there, if one is (FX106); a later one from the same page
/// carries only those and the address.
/// </summary>
public sealed record Page(
    string Url,
    string Title,
    string Text,
    IReadOnlyList<PageArea> Areas,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    PageArea? InView = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    PageArea? Open = null);
