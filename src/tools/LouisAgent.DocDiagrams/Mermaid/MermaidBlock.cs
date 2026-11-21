namespace LouisAgent.DocDiagrams.Mermaid;

/// <summary>
/// One Mermaid flowchart in a doc: the lines of its fence, counted from one, what is between them, and the heading
/// above it, which names its picture.
/// </summary>
public sealed record MermaidBlock(int Start, int End, string Body, string Heading);
