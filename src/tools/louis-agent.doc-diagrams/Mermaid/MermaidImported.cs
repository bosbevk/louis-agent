namespace LouisAgent.DocDiagrams.Mermaid;

/// <summary>One flowchart imported: the JSON written, the block it came from, and the link in its place.</summary>
public sealed record MermaidImported(string Json, MermaidBlock Block, string Link);
