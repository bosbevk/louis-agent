namespace LouisAgent.Web.Chats;

/// <summary>A file attached to a message: its name and its text.</summary>
public sealed record Attachment(string Name, string Content);
