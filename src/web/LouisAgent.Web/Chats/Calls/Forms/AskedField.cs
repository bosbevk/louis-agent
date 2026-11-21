namespace LouisAgent.Web.Chats.Calls.Forms;

/// <summary>One field of a form the agent asked with: the label over its box, and the kind of box.</summary>
public sealed record AskedField(string Label, AskedKind Kind);
