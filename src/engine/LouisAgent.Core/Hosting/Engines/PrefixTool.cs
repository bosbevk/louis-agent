using System.Text.Json;

namespace LouisAgent.Core.Hosting.Engines;

/// <summary>
/// One tool as the model reads it in every request's prefix: its name, what it does and its arguments' schema, written
/// out only to estimate the prefix's size.
/// </summary>
public sealed record PrefixTool(string Name, string Description, JsonElement JsonSchema);
