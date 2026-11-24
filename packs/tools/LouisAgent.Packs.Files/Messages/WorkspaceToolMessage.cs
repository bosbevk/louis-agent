
using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Packs.Files.Messages.Holes.Directories;
using LouisAgent.Packs.Files.Messages.Holes.Files;
using LouisAgent.Packs.Files.Messages.Holes.Moves;
using LouisAgent.Packs.Files.Messages.Holes.Paths;
using LouisAgent.Packs.Files.Messages.Holes.Search;

namespace LouisAgent.Packs.Files.Messages;
// The message enums (src/docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format, and its holes record names them.

/// <summary>
/// Strings for the workspace toolset (files and folders), returned to the model: what went wrong and what to do
/// instead.
/// </summary>
public enum WorkspaceToolMessage
{
    [Description("Invalid file path.")]
    InvalidFilePath,

    [Description("Error searching for files: {Reason}")]
    [Holes(typeof(FileSearchFailedHoles))]
    FileSearchFailed,

    [Description("Directory not found: {Path}")]
    [Holes(typeof(DirectoryNotFoundHoles))]
    DirectoryNotFound,

    [Description("Directory is empty.")]
    DirectoryEmpty,

    [Description("File is larger than the 1 MB reading limit.")]
    FileTooLarge,

    [Description("Search query cannot be empty.")]
    WorkspaceSearchQueryEmpty,

    [Description("No matches found.")]
    NoMatches,

    // A line too long for a hit, such as a recording's one-line JSON, shown around the match (FX242).
    [Description("{Shown} [the line is {Length} characters, cut here; read the file for all of it]")]
    [Holes(typeof(HitLineCutHoles))]
    HitLineCut,

    [Description("Writing this sensitive file is not allowed.")]
    SensitiveWrite,

    [Description("Wrote {Path} ({Length} characters).")]
    [Holes(typeof(FileWrittenHoles))]
    FileWritten,

    [Description("Directory already exists: {Path}")]
    [Holes(typeof(DirectoryExistsAlreadyHoles))]
    DirectoryExistsAlready,

    [Description("Created directory: {Path}")]
    [Holes(typeof(DirectoryCreatedHoles))]
    DirectoryCreated,

    [Description("Deleting this sensitive file is not allowed.")]
    SensitiveDelete,

    [Description("Deleted file: {Path}")]

    [Holes(typeof(FileDeletedHoles))]
    FileDeleted,

    [Description("This directory contains a sensitive file and cannot be deleted.")]
    SensitiveDirectoryDelete,

    [Description("Deleted directory: {Path}")]
    [Holes(typeof(DirectoryDeletedHoles))]
    DirectoryDeleted,

    [Description("Renaming this sensitive file is not allowed.")]
    SensitiveRename,

    [Description("Path not found: {Path}")]
    [Holes(typeof(PathNotFoundHoles))]
    PathNotFound,

    [Description("Destination already exists: {Path}")]
    [Holes(typeof(DestinationExistsHoles))]
    DestinationExists,

    [Description("Copying this sensitive file is not allowed.")]
    SensitiveCopy,

    [Description("Source file not found: {Path}")]
    [Holes(typeof(SourceFileNotFoundHoles))]
    SourceFileNotFound,

    [Description("Copied {Source} to {Destination}")]
    [Holes(typeof(FileCopiedHoles))]
    FileCopied,

    [Description("Writing to this sensitive file is not allowed.")]
    SensitiveAppend,

    [Description("Appended to {Path} ({Length} characters).")]
    [Holes(typeof(FileAppendedHoles))]
    FileAppended,

    [Description("No files matching pattern '{Pattern}' found.")]
    [Holes(typeof(NoFilesMatchingHoles))]
    NoFilesMatching,

    [Description("Found {Count} file(s) matching '{Pattern}':\n")]
    [Holes(typeof(FilesFoundHoles))]
    FilesFound,

    [Description("File exists: {Path}")]

    [Holes(typeof(FileExistsHoles))]
    FileExists,

    [Description("Directory exists: {Path}")]
    [Holes(typeof(DirectoryExistsHoles))]
    DirectoryExists,

    [Description("Path does not exist: {Path}")]
    [Holes(typeof(PathDoesNotExistHoles))]
    PathDoesNotExist,

    [Description("Renamed file from {From} to {To}")]
    [Holes(typeof(FileRenamedHoles))]
    FileRenamed,

    [Description("Renamed directory from {From} to {To}")]
    [Holes(typeof(DirectoryRenamedHoles))]
    DirectoryRenamed,

    [Description("[truncated at 200 entries]")]
    ListTruncated,

    [Description("\n[results truncated at 500 files]")]
    FindTruncated,

    [Description("Directory tree for {Path} (depth: {Depth}):\n")]
    [Holes(typeof(DirectoryTreeHoles))]
    DirectoryTree,
}
