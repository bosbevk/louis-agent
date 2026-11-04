
using System.ComponentModel;

namespace LouisAgent.Core.Tools.Workspace.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the workspace toolset (files and folders), returned to the model: what went wrong and what to do instead.</summary>
public enum WorkspaceToolMessage
{
    [Description("Invalid file path.")] InvalidFilePath,

    [Description("Error searching for files: {Reason}")]
    FileSearchFailed,

    [Description("Directory not found: {Path}")]
    DirectoryNotFound,

    [Description("Directory is empty.")] DirectoryEmpty,

    [Description("File is larger than the 1 MB reading limit.")]
    FileTooLarge,

    [Description("Search query cannot be empty.")]
    WorkspaceSearchQueryEmpty,

    [Description("No matches found.")] NoMatches,

    [Description("Writing this sensitive file is not allowed.")]
    SensitiveWrite,

    [Description("Wrote {Path} ({Length} characters).")]
    FileWritten,

    [Description("Directory already exists: {Path}")]
    DirectoryExistsAlready,

    [Description("Created directory: {Path}")]
    DirectoryCreated,

    [Description("Deleting this sensitive file is not allowed.")]
    SensitiveDelete,

    [Description("Deleted file: {Path}")] FileDeleted,

    [Description("This directory contains a sensitive file and cannot be deleted.")]
    SensitiveDirectoryDelete,

    [Description("Deleted directory: {Path}")]
    DirectoryDeleted,

    [Description("Renaming this sensitive file is not allowed.")]
    SensitiveRename,

    [Description("Path not found: {Path}")]
    PathNotFound,

    [Description("Destination already exists: {Path}")]
    DestinationExists,

    [Description("Copying this sensitive file is not allowed.")]
    SensitiveCopy,

    [Description("Source file not found: {Path}")]
    SourceFileNotFound,

    [Description("Copied {Source} to {Destination}")]
    FileCopied,

    [Description("Writing to this sensitive file is not allowed.")]
    SensitiveAppend,

    [Description("Appended to {Path} ({Length} characters).")]
    FileAppended,

    [Description("No files matching pattern '{Pattern}' found.")]
    NoFilesMatching,

    [Description("File exists: {Path}")] FileExists,

    [Description("Directory exists: {Path}")]
    DirectoryExists,

    [Description("Path does not exist: {Path}")]
    PathDoesNotExist,

    [Description("Renamed file from {From} to {To}")]
    FileRenamed,

    [Description("Renamed directory from {From} to {To}")]
    DirectoryRenamed,

    [Description("[truncated at 200 entries]")]
    ListTruncated,

    [Description("\n[results truncated at 500 files]")]
    FindTruncated,

    [Description("Directory tree for {Path} (depth: {Depth}):\n")]
    DirectoryTree,
}
