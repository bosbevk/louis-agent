using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.Tools;

/// <summary>
/// A short mark of a tool list: each tool's name, description and schema, in order, hashed. Two lists with the same
/// mark sent the model the same tools, so a chat resumed on a list with another mark knows its tools moved since.
/// </summary>
/// <param name="Value">The SHA-256 of the list, as lowercase hex.</param>
public sealed record ToolsFingerprint(string Value)
{
    /// <summary>The mark of the tools given, in the order they are sent.</summary>
    public static ToolsFingerprint Of(IEnumerable<AITool> tools)
    {
        var text = new StringBuilder();
        foreach (var tool in tools)
        {
            // A tool's schema only exists on a function; what the model reads of any tool is its name and description.
            string schema = tool is AIFunction function ? function.JsonSchema.GetRawText() : string.Empty;
            text.Append(tool.Name).Append('\n').Append(tool.Description).Append('\n').Append(schema).Append('\n');
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()));
        return new ToolsFingerprint(Convert.ToHexStringLower(hash));
    }
}
