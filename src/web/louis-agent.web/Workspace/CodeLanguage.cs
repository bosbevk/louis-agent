using LouisAgent.Choices;
using LouisAgent.Web.Shared.Api;

namespace LouisAgent.Web.Workspace;

/// <summary>The highlight.js language for a workspace file (W2b, from FileViewer).</summary>
public static class CodeLanguage
{
    /// <summary>highlight.js language for a file name; null shows it as plain text.</summary>
    public static string? For(string fileName)
    {
        if (fileName.StartsWith("Dockerfile", StringComparison.OrdinalIgnoreCase)) return "dockerfile";
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".cs" or ".csx" => "csharp",
            // Razor is mostly markup; highlighting it as HTML reads better than as C#.
            ".razor" or ".cshtml" or ".html" or ".htm" or ".xml" or ".csproj" or ".props" or ".targets"
                or ".svg" => "xml",
            ".json" => "json",
            ".yml" or ".yaml" => "yaml",
            ".md" => "markdown",
            ".ps1" or ".psm1" or ".psd1" => ScriptLanguage.PowerShell.Wire(),
            ".sh" or ".bash" => ScriptLanguage.Bash.Wire(),
            ".py" => ScriptLanguage.Python.Wire(),
            ".js" or ".mjs" => "javascript",
            ".ts" => "typescript",
            ".css" => "css",
            ".scss" => "scss",
            ".sql" => "sql",
            ".toml" or ".ini" or ".editorconfig" => "ini",
            ".go" => "go",
            ".java" => "java",
            ".diff" or ".patch" => "diff",
            _ => null,
        };
    }
}
