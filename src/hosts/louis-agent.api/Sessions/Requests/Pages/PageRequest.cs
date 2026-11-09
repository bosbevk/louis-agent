namespace LouisAgent.Api.Sessions.Requests.Pages;

/// <summary>
/// The page the visitor is reading when they send a message (F115): its address, its title and its visible text, so
/// the model sees what they see. The web sends it with the first message of a chat and again after a move to another
/// page. Every message carries the section on the visitor's screen (F118) and the card open there, if one is
/// (FX106); a later one from the same page carries only those and the address. The page's links (FX120) come with
/// its words, each as the address it opens and its text, in an area's shape.
/// </summary>
internal sealed record PageRequest(
    string? Url,
    string? Title,
    string? Text,
    List<PageAreaRequest>? Areas = null,
    PageAreaRequest? InView = null,
    PageAreaRequest? Open = null,
    List<PageAreaRequest>? Links = null);
