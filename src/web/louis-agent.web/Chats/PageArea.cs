namespace LouisAgent.Web.Chats;

/// <summary>One named area of the page the visitor is reading (F117): a section's id and its heading, so the guide knows where it can send them.</summary>
public sealed record PageArea(string Id, string Heading);
