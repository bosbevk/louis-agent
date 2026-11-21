
using System.Text.Json;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Sessions.Replay.Store;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Llm.Replay;
/// <summary>
/// What a model request is, as a recording keys it (F18 step 4e, ADR 0040: a value). Its fields are the recording format, written
/// snake_case by <see cref="ToAsk"/>: changing them makes earlier recordings miss. Every hash is a SHA-256 as 64 lowercase hex characters
/// in <see cref="PromptHash"/>'s encoding.
/// <list type="bullet">
/// <item><see cref="Model"/>: the options' model, else the inner client's default.</item>
/// <item><see cref="System"/>: the leading system messages and the instructions (<see cref="PromptHash.SystemOf"/>).</item>
/// <item><see cref="Tools"/>: each tool in the order sent.</item>
/// <item><see cref="Settings"/>: the rest of the options, as Microsoft.Extensions.AI serializes them.</item>
/// <item><see cref="Messages"/>: each message after the leading system ones, over its role, author, contents and additional properties
/// and never its <c>CreatedAt</c> or <c>MessageId</c>: the tool loop stamps the time it ran and a random id on the messages the host
/// appends, and the next turn's first request carries them, so a key that counted them would miss on every turn after a tool call.</item>
/// </list>
/// Whether the request was streamed is left out: the cache stores both ways as one response. The messages are only read (ADR 0008).
/// </summary>
public sealed record ModelAsk(
    string? Model,
    string System,
    IReadOnlyList<ModelAsk.ToolHash> Tools,
    string Settings,
    IReadOnlyList<ModelAsk.MessageHash> Messages)
{
    /// <summary>One tool of a request: its name, and the hash of its name, description and schema (<see cref="PromptHash.ToolOf"/>).</summary>
    public sealed record ToolHash(string Name, string Hash);

    /// <summary>One message of a request after the system prompt: its role, and the hash of what the model reads in it.</summary>
    public sealed record MessageHash(string Role, string Hash);

    public static ModelAsk Of(IEnumerable<ChatMessage> messages, ChatOptions? options, string? defaultModel)
    {
        List<ChatMessage> all = [.. messages];
        return new ModelAsk(
            options?.ModelId ?? defaultModel,
            PromptHash.SystemOf(all, options),
            [.. (options?.Tools ?? []).Select(tool => new ToolHash(tool.Name, PromptHash.ToolOf(tool)))],
            SettingsHash(options),
            [
                .. all.SkipWhile(m => m.Role == ChatRole.System)
                    .Select(m => new MessageHash(m.Role.Value, MessageDigest(m)))
            ]);
    }

    public ReplayAsk ToAsk() => ReplayAsk.Of(ReplayKind.Model, this);

    /// <summary>The ask a recording holds, or null when <paramref name="ask"/> is of another kind or isn't a model ask.</summary>
    public static ModelAsk? From(ReplayAsk ask)
    {
        if (ask.Kind != ReplayKind.Model) return null;
        try
        {
            return JsonSerializer.Deserialize<ModelAsk>(ask.Key, ReplayStore.Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string SettingsHash(ChatOptions? options)
    {
        ChatOptions rest = options?.Clone() ?? new ChatOptions();
        (rest.Tools, rest.Instructions, rest.ModelId) = (null, null, null);
        JsonElement element = JsonSerializer.SerializeToElement(rest, AIJsonUtilities.DefaultOptions);
        return PromptHash.Digest(element.WriteTo);
    }

    private static string MessageDigest(ChatMessage message)
    {
        JsonElement element = JsonSerializer.SerializeToElement(
            new MessageKey(message.Role.Value, message.AuthorName, message.Contents, message.AdditionalProperties),
            AIJsonUtilities.DefaultOptions);
        return PromptHash.Digest(element.WriteTo);
    }

    private sealed record MessageKey(
        string Role,
        string? Author,
        IList<AIContent> Contents,
        AdditionalPropertiesDictionary? AdditionalProperties);
}
