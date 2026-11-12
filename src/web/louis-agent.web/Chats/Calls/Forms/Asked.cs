namespace LouisAgent.Web.Chats.Calls.Forms;

/// <summary>What an AskFor call asked the visitor: the question, and the fields drawn as boxes under it.</summary>
public sealed record Asked(string Question, IReadOnlyList<AskedField> Fields);
