namespace LouisAgent.Api.Sessions.Requests.Pages;

/// <summary>
/// The page the visitor is reading when they send a message (F115): its address, its title and its visible text, so
/// the model sees what they see. The web sends it with the first message of a chat and again after a move to another
/// page. Every message carries the section on the visitor's screen (F118); a later one from the same page carries
/// only that and the address.
/// </summary>
internal sealed record PageRequest(
    string? Url,
    string? Title,
    string? Text,
    List<PageAreaRequest>? Areas = null,
    PageAreaRequest? InView = null);
