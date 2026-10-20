namespace LouisAgent.Core.Agent.Turns;

/// <summary>A tool call as a progress display shows it (S, CL15, from AgentEngine).</summary>
public static class ToolCallText
{
    /// <summary>Short, single-line view of a tool call's arguments for progress displays.</summary>
    public static string Describe(IDictionary<string, object?>? arguments)
    {
        if (arguments is null || arguments.Count == 0) return string.Empty;
        return string.Join(", ", arguments.Select(a =>
        {
            string value = a.Value?.ToString()?.ReplaceLineEndings(" ") ?? "null";
            return $"{a.Key}={(value.Length > 60 ? value[..60] + "..." : value)}";
        }));
    }
}
