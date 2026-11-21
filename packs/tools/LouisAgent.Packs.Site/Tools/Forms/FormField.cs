namespace LouisAgent.Packs.Site.Tools.Forms;

/// <summary>One field of a form the agent asks with: the label over its box, and what kind of answer goes in it.</summary>
public sealed record FormField(string Label, FieldKind Kind);
