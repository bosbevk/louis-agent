namespace LouisAgent.Web.Docs.Contents;

/// <summary>A group of a docs site's pages under one name in the menu, such as "Start" or "Understand".</summary>
public sealed record DocsGroup(string Label, IReadOnlyList<DocsEntry> Entries);
