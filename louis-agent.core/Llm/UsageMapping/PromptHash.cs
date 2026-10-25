namespace LouisAgent.Core.Llm.UsageMapping;

using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.AI;

/// <summary>
/// The version of a prompt, as its own content's hash (F18 step 4c, skill 3's prompt versioning): the same system prompt and
/// tools always hash the same, and one changed word gives a new hash.
/// <para>
/// Hashed: the leading system messages (their texts, in order), <see cref="ChatOptions.Instructions"/>, and
/// <see cref="ChatOptions.Tools"/> in the order sent (each one's name, description and, for an
/// <see cref="AIFunctionDeclaration"/>, its JSON schema). Not hashed: the conversation after the leading system messages
/// (a later system message included), the model, and the settings (temperature, output limit, thinking).
/// </para>
/// <para>
/// The encoding is a contract: compact UTF-8 JSON, escaped only where JSON requires (not as the default encoder, which
/// writes <c>ê</c> and <c>'</c> as escapes), then SHA-256 as 64 lowercase hex characters. Changing it changes every hash,
/// and the runs recorded before the change stop comparing; <c>PromptHashTests</c> pins it. The messages are only read (ADR 0008).
/// </para>
/// </summary>
public static class PromptHash
{
    private static readonly JsonWriterOptions Compact = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string Of(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream, Compact))
        {
            json.WriteStartObject();
            json.WriteStartArray("system");
            foreach (ChatMessage message in messages.TakeWhile(m => m.Role == ChatRole.System))
                json.WriteStringValue(message.Text);
            json.WriteEndArray();
            json.WriteString("instructions", options?.Instructions);
            json.WriteStartArray("tools");
            foreach (AITool tool in options?.Tools ?? [])
                WriteTool(json, tool);
            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    private static void WriteTool(Utf8JsonWriter json, AITool tool)
    {
        json.WriteStartObject();
        json.WriteString("name", tool.Name);
        json.WriteString("description", tool.Description);
        json.WritePropertyName("schema");
        if (tool is AIFunctionDeclaration { JsonSchema: { ValueKind: not JsonValueKind.Undefined } schema }) schema.WriteTo(json);
        else json.WriteNullValue();
        json.WriteEndObject();
    }
}
