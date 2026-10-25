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

    public static string Of(IEnumerable<ChatMessage> messages, ChatOptions? options) =>
        Digest(json =>
        {
            json.WriteStartObject();
            WriteSystem(json, messages, options);
            json.WriteStartArray("tools");
            foreach (AITool tool in options?.Tools ?? [])
                WriteTool(json, tool);
            json.WriteEndArray();
            json.WriteEndObject();
        });

    /// <summary>
    /// The system part alone (F18 step 4e, the replay key's <c>system</c>): the leading system messages and the instructions, hashed the
    /// way <see cref="Of"/> hashes them, without the tools.
    /// </summary>
    public static string SystemOf(IEnumerable<ChatMessage> messages, ChatOptions? options) =>
        Digest(json =>
        {
            json.WriteStartObject();
            WriteSystem(json, messages, options);
            json.WriteEndObject();
        });

    /// <summary>One tool's name, description and schema, hashed the way <see cref="Of"/> hashes it (step 4e's per-tool key).</summary>
    public static string ToolOf(AITool tool) => Digest(json => WriteTool(json, tool));

    /// <summary>The SHA-256 of what <paramref name="write"/> writes, in this class's encoding: the one place that contract lives.</summary>
    internal static string Digest(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream, Compact))
            write(json);

        return Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    private static void WriteSystem(Utf8JsonWriter json, IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        json.WriteStartArray("system");
        foreach (ChatMessage message in messages.TakeWhile(m => m.Role == ChatRole.System))
            json.WriteStringValue(message.Text);
        json.WriteEndArray();
        json.WriteString("instructions", options?.Instructions);
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
