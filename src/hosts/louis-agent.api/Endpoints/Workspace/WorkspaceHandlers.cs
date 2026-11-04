using System.Text;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Workspace;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Guardrails.Workspace;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Workspace.Listing;
using Microsoft.AspNetCore.Http.HttpResults;
using EntriesAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Workspace.EntriesReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using FileAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Workspace.FileReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Workspace;

/// <summary>
/// Read-only browsing of the agent's workspace for the web app, as named methods. Paths go through the same checks as
/// the agent's own file tools: they must stay inside the workspace, can't follow symbolic links, and secret files
/// (.env, keys, certificates) are listed but never returned.
/// </summary>
/// <remarks>
/// A handler class per endpoint file (Introduce Parameter Object, Fowler): the routes share no state of their own,
/// only the workspace and the engine the framework hands each call, so the class holds nothing. Each returns
/// <c>TypedResults</c> with its answers declared, written with <c>WorkspaceJson.Web</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class WorkspaceHandlers
{
    // Bigger files aren't useful to read in a browser and would be slow to render.
    private const long MaxFileBytes = 1_000_000;

    /// <summary>A folder's entries: its folders, then its files, each by name.</summary>
    public EntriesAnswer GetEntries(string? path, WorkspaceBoundary workspace, AgentEngine engine)
    {
        if (Resolve(workspace, path ?? "", out string? fullPath) is { } invalid) return invalid;
        if (!Directory.Exists(fullPath))
            return ApiErrors.NotFound(MessageText.Format(ApiUserText.NoFolder, new { Path = path }));

        var directory = new DirectoryInfo(fullPath!);
        var folders = directory.EnumerateDirectories()
            .Where(d => !WorkspaceFilter.ExcludedDirectories.Contains(d.Name)
                        && !d.Attributes.HasFlag(FileAttributes.ReparsePoint))
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .Select(d => new WorkspaceEntryRow(d.Name, Relative(engine, d.FullName), "directory", null, false));
        var files = directory.EnumerateFiles()
            .Where(f => !f.Attributes.HasFlag(FileAttributes.ReparsePoint))
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .Select(f => new WorkspaceEntryRow(f.Name, Relative(engine, f.FullName), "file", f.Length,
                SensitiveFiles.IsSensitive(f.FullName)));

        var reply = new EntriesReply(Relative(engine, fullPath!), [.. folders.Concat(files)]);
        return TypedResults.Json(reply, WorkspaceJson.Web);
    }

    /// <summary>
    /// A text file: refused when it's missing, secret, over a megabyte, or not text (a zero byte in its first 8,000).
    /// </summary>
    public FileAnswer GetFile(string? path, WorkspaceBoundary workspace, AgentEngine engine)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ApiErrors.BadRequest(MessageText.Format(ApiUserText.PathRequired));
        if (Resolve(workspace, path, out string? fullPath) is { } invalid) return invalid;

        var file = new FileInfo(fullPath!);
        if (!file.Exists)
            return ApiErrors.NotFound(MessageText.Format(ApiUserText.NoFile, new { Path = path }));
        if (SensitiveFiles.IsSensitive(file.FullName))
            return ApiErrors.Forbidden(MessageText.Format(ApiUserText.SecretFileHidden));
        if (file.Length > MaxFileBytes)
            return ApiErrors.TooLarge(MessageText.Format(ApiUserText.FileTooLarge, new { file.Name }));

        byte[] bytes = File.ReadAllBytes(file.FullName);
        if (bytes.AsSpan(0, Math.Min(bytes.Length, 8_000)).Contains((byte)0))
            return ApiErrors.UnsupportedMediaType(MessageText.Format(ApiUserText.BinaryFile, new { file.Name }));

        // The text without a byte-order mark, which a browser would show as a stray character.
        string content = new UTF8Encoding(false).GetString(bytes).TrimStart('﻿');
        return TypedResults.Json(new FileReply(Relative(engine, file.FullName), file.Name, file.Length, content),
            WorkspaceJson.Web);
    }

    /// <summary>
    /// The workspace's full path for a relative one, or the 400 saying why the workspace refuses it.
    /// </summary>
    private static JsonHttpResult<ApiError>? Resolve(WorkspaceBoundary workspace, string path, out string? fullPath)
    {
        try
        {
            fullPath = workspace.Resolve(path);
            return null;
        }
        catch (ArgumentException ex)
        {
            fullPath = null;
            // The workspace's reason, without .NET's "(Parameter 'x')" tail.
            string message = ex.ParamName is null
                ? ex.Message
                : ex.Message.Replace($" (Parameter '{ex.ParamName}')", "");
            return ApiErrors.BadRequest(message);
        }
    }

    /// <summary>A path as the agent's tools take it: relative, with forward slashes, and "" for the root.</summary>
    private static string Relative(AgentEngine engine, string fullPath)
    {
        string relative = Path.GetRelativePath(engine.WorkspaceRoot, fullPath).Replace('\\', '/');
        return relative == "." ? "" : relative;
    }
}
