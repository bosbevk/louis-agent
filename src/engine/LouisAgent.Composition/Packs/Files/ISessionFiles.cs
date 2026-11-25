namespace LouisAgent.Composition.Packs.Files;

/// <summary>
/// The files sent in this session, for a tool that reads one (F171.1). The model knows each by its name, and a tool
/// asks for it by that name; the bytes never pass through the conversation.
/// </summary>
/// <remarks>
/// Separated Interface (Fowler, PoEAA), as <see cref="IPackContext" /> is: the engine keeps the files, and a pack sees
/// only this, so it needs no part of the engine and nothing of Microsoft.Extensions.AI.
/// </remarks>
public interface ISessionFiles
{
    /// <summary>The file sent with this name, or none when the session has no such file.</summary>
    SessionFile? Find(string name);

    /// <summary>Lets the session drop a file a tool is done with; a name it doesn't have is no error.</summary>
    void Forget(string name);
}
