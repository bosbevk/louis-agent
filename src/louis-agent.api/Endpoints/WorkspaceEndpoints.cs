using LouisAgent.Core.Guardrails.Workspace;
using System.Text;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Tools.Workspace.Listing;
using LouisAgent.Core.Messages;
using LouisAgent.Api.Messages;

namespace LouisAgent.Api.Endpoints;

/// <summary>Errors in Claude's shape: { "type": "error", "error": { "type", "message" } }.</summary>
/// <summary>
/// Read-only browsing of the agent's workspace for the web app. Paths go through the same checks as the agent's own
/// file tools: they must stay inside the workspace, can't follow symbolic links, and secret files (.env, keys,
/// certificates) are listed but never returned.
/// </summary>
internal static class WorkspaceEndpoints
{
    // Bigger files aren't useful to read in a browser and would be slow to render.
    private const long MaxFileBytes = 1_000_000;

    public static void MapWorkspace(this WebApplication app)
    {
        app.MapGet("/workspace/entries", (string? path, WorkspaceBoundary workspace, AgentEngine engine) =>
        {
            if (Resolve(workspace, path ?? "", out string? fullPath) is { } invalid) return invalid;
            if (!Directory.Exists(fullPath))
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoFolder, new { Path = path }));

            var directory = new DirectoryInfo(fullPath!);
            var folders = directory.EnumerateDirectories()
                .Where(d => !WorkspaceFilter.ExcludedDirectories.Contains(d.Name) && !d.Attributes.HasFlag(FileAttributes.ReparsePoint))
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .Select(d => new { name = d.Name, path = Relative(engine, d.FullName), type = "directory", size = (long?)null, sensitive = false });
            var files = directory.EnumerateFiles()
                .Where(f => !f.Attributes.HasFlag(FileAttributes.ReparsePoint))
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .Select(f => new { name = f.Name, path = Relative(engine, f.FullName), type = "file", size = (long?)f.Length, sensitive = SensitiveFiles.IsSensitive(f.FullName) });

            return Results.Json(new { path = Relative(engine, fullPath!), entries = folders.Concat(files) });
        });

        app.MapGet("/workspace/file", (string? path, WorkspaceBoundary workspace, AgentEngine engine) =>
        {
            if (string.IsNullOrWhiteSpace(path))
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", MessageText.Format(ApiUserText.PathRequired));
            if (Resolve(workspace, path, out string? fullPath) is { } invalid) return invalid;

            var file = new FileInfo(fullPath!);
            if (!file.Exists)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoFile, new { Path = path }));
            if (SensitiveFiles.IsSensitive(file.FullName))
                return ApiErrors.Result(StatusCodes.Status403Forbidden, "permission_error", MessageText.Format(ApiUserText.SecretFileHidden));
            if (file.Length > MaxFileBytes)
                return ApiErrors.Result(StatusCodes.Status413PayloadTooLarge, "request_too_large", $"{file.Name} is larger than 1 MB.");

            byte[] bytes = File.ReadAllBytes(file.FullName);
            if (bytes.AsSpan(0, Math.Min(bytes.Length, 8_000)).Contains((byte)0))
                return ApiErrors.Result(StatusCodes.Status415UnsupportedMediaType, "invalid_request_error", $"{file.Name} is a binary file.");

            return Results.Json(new
            {
                path = Relative(engine, file.FullName),
                name = file.Name,
                size = file.Length,
                content = new UTF8Encoding(false).GetString(bytes).TrimStart('﻿'),
            });
        });
    }

    private static IResult? Resolve(WorkspaceBoundary workspace, string path, out string? fullPath)
    {
        try
        {
            fullPath = workspace.Resolve(path);
            return null;
        }
        catch (ArgumentException ex)
        {
            fullPath = null;
            string message = ex.ParamName is null ? ex.Message : ex.Message.Replace($" (Parameter '{ex.ParamName}')", "");
            return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", message);
        }
    }

    // Forward slashes, "" for the root: the same form the agent's tools accept.
    private static string Relative(AgentEngine engine, string fullPath)
    {
        string relative = Path.GetRelativePath(engine.WorkspaceRoot, fullPath).Replace('\\', '/');
        return relative == "." ? "" : relative;
    }
}
