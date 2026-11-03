namespace LouisAgent.Evals.Readers;

/// <summary>One line of the reader catalogue's listing: a reader's name and the kind of value it reads.</summary>
public sealed record ListedReader(string Name, ReaderKind Kind);
