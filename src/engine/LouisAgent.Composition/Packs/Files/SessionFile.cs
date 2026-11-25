namespace LouisAgent.Composition.Packs.Files;

/// <summary>A file the session keeps for its tools: the name the model knows it by, its kind and its bytes.</summary>
/// <param name="Name">The name it was sent with, which a tool is given to find it.</param>
/// <param name="MediaType">What kind of file it is.</param>
/// <param name="Bytes">The file itself.</param>
public sealed record SessionFile(string Name, FileMediaType MediaType, ReadOnlyMemory<byte> Bytes);
