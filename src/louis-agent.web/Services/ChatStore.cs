using System.Text.Json;
using Microsoft.JSInterop;

namespace louis_agent.web.Services;

/// <summary>The API key for louis-agent.api, if it requires one (kept in this browser only).</summary>
public sealed class Settings
{
    public string? ApiKey { get; set; }
}

/// <summary>Chats and settings saved in the browser's localStorage, so a reload keeps the conversation.</summary>
public sealed class ChatStore(IJSRuntime js, Settings settings)
{
    private const string ChatsKey = "louis-agent.chats";
    private const string ApiKeyKey = "louis-agent.apiKey";

    // localStorage holds ~5 MB per site; tool output is the bulk of a chat, so only its start is kept.
    private const int MaxStoredToolResultChars = 4_000;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public List<Chat> Chats { get; private set; } = [];
    public bool Loaded { get; private set; }

    /// <summary>The saved API key, for the settings field.</summary>
    public string? ApiKeyDraft => settings.ApiKey;

    public async Task LoadAsync()
    {
        Loaded = true;
        settings.ApiKey = await js.InvokeAsync<string?>("localStorage.getItem", ApiKeyKey);
        string? json = await js.InvokeAsync<string?>("localStorage.getItem", ChatsKey);
        try
        {
            Chats = json is null ? [] : JsonSerializer.Deserialize<List<Chat>>(json, Json) ?? [];
        }
        catch (JsonException)
        {
            Chats = [];
        }
    }

    public Chat? Find(string? sessionId) => Chats.FirstOrDefault(c => c.SessionId == sessionId);

    public async Task SaveAsync()
    {
        foreach (var block in Chats.SelectMany(c => c.Turns).SelectMany(t => t.Blocks))
        {
            if (block.ToolResult is { Length: > MaxStoredToolResultChars } result)
                block.ToolResult = result[..MaxStoredToolResultChars] + $"\n... [{result.Length - MaxStoredToolResultChars:N0} more chars not saved]";
        }

        Chats = Chats.OrderByDescending(c => c.UpdatedAt).ToList();
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", ChatsKey, JsonSerializer.Serialize(Chats, Json));
        }
        catch (JSException)
        {
            // Storage full: drop the oldest chats until it fits.
            while (Chats.Count > 1)
            {
                Chats.RemoveAt(Chats.Count - 1);
                try
                {
                    await js.InvokeVoidAsync("localStorage.setItem", ChatsKey, JsonSerializer.Serialize(Chats, Json));
                    return;
                }
                catch (JSException) { }
            }
        }
    }

    public async Task SaveApiKeyAsync(string? apiKey)
    {
        settings.ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
        if (settings.ApiKey is null) await js.InvokeVoidAsync("localStorage.removeItem", ApiKeyKey);
        else await js.InvokeVoidAsync("localStorage.setItem", ApiKeyKey, settings.ApiKey);
    }
}
