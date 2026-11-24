namespace LouisAgent.Packs.Files.Messages.Holes.Search;

/// <summary>How many files a name search found, and the pattern they match.</summary>
public sealed record FilesFoundHoles(int Count, string Pattern);
