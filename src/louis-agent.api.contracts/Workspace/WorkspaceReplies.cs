namespace LouisAgent.Api.Contracts.Workspace;

/// <summary>A workspace folder's entries: its path, then its folders and its files.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record EntriesReply(string Path, IReadOnlyList<WorkspaceEntryRow> Entries);

/// <summary>
/// One entry: its name and path, whether it's a folder or a file, a file's size, and whether it's a secret file
/// (listed, never opened).
/// </summary>
public sealed record WorkspaceEntryRow(string Name, string Path, string Type, long? Size, bool Sensitive);

/// <summary>A text file from the workspace: its path, name, size and content.</summary>
public sealed record FileReply(string Path, string Name, long Size, string Content);
