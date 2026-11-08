namespace LouisAgent.Api.Sessions.Requests;

/// <summary>
/// The page the visitor is reading when they send a message (F115): its address, its title and its visible text, so
/// the model sees what they see. The web sends it with the first message of a chat and again after a move to another
/// page.
/// </summary>
internal sealed record PageRequest(string? Url, string? Title, string? Text);
