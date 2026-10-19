using System.Text.Json;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Shared.Storage;

using LouisAgent.Web.Shared.Chat;

/// <summary>Chats and settings saved in the browser's localStorage, so a reload keeps the conversation.</summary>
public sealed class ChatStore(IJSRuntime js, ChatSettings settings)
{
    private const string ChatsKey = "louis-agent.chats";
    private const string ApiKeyKey = "louis-agent.apiKey";
    private const string AdminKeyKey = "louis-agent.adminKey";

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
        settings.AdminKey = await js.InvokeAsync<string?>("localStorage.getItem", AdminKeyKey);
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

    /// <summary>The admin portal's key, in its own entry: the chat's key is left as it is (F31).</summary>
    public async Task SaveAdminKeyAsync(string? adminKey)
    {
        settings.AdminKey = string.IsNullOrWhiteSpace(adminKey) ? null : adminKey.Trim();
        if (settings.AdminKey is null) await js.InvokeVoidAsync("localStorage.removeItem", AdminKeyKey);
        else await js.InvokeVoidAsync("localStorage.setItem", AdminKeyKey, settings.AdminKey);
    }

    public async Task SaveApiKeyAsync(string? apiKey)
    {
        settings.ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
        if (settings.ApiKey is null) await js.InvokeVoidAsync("localStorage.removeItem", ApiKeyKey);
        else await js.InvokeVoidAsync("localStorage.setItem", ApiKeyKey, settings.ApiKey);
    }
}
