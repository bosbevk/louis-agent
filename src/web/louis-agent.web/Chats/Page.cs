namespace LouisAgent.Web.Chats;

/// <summary>
/// The page the visitor is reading when they send a message (F115): its address, its title and its visible text, so
/// the guide can answer about what they see. The widget sets it when it opens and when the route changes; the runner
/// sends it with the next message and forgets it.
/// </summary>
public sealed record Page(string Url, string Title, string Text);
