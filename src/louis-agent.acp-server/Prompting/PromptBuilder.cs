namespace LouisAgent.AcpServer.Prompting;

using System.Globalization;
using System.Text.Json;
using LouisAgent.AcpServer.Messages;
using LouisAgent.AcpServer.Paths;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>The model's prompt from the ACP content blocks (S, CL15, from AcpServer).</summary>
internal sealed class PromptBuilder(ContainerPaths paths)
{
    private static readonly ILogger Log = AgentLogging.For<PromptBuilder>();
    private const string Tag = "ACP";

    // Large enough for any normal source file, small enough that one attachment can't fill the context window.
    private const int MaxAttachmentChars = 100_000;

    /// <summary>
    /// Text, embedded resources (inline file contents) and files the user attached. Rider sends attachments and the
    /// file open in the editor both as resource_link; only the attached ones are read in, so the open file doesn't get
    /// pulled into every prompt.
    /// </summary>
    public string Build(JsonElement parameters)
    {
        var promptParts = new List<string>();
        if (!parameters.TryGetProperty("prompt", out JsonElement promptItems) || promptItems.ValueKind != JsonValueKind.Array)
            return string.Empty;

        foreach (var item in promptItems.EnumerateArray())
        {
            switch (item.TryGetProperty("type", out JsonElement typeElement) ? typeElement.GetString() : null)
            {
                case "text" when item.TryGetProperty("text", out JsonElement textElement):
                    if (textElement.GetString() is { Length: > 0 } text) promptParts.Add(text);
                    break;

                case "resource" when item.TryGetProperty("resource", out JsonElement resource):
                    string embeddedUri = resource.TryGetProperty("uri", out JsonElement u) ? u.GetString() ?? "" : "";
                    if (resource.TryGetProperty("text", out JsonElement embedded) && embedded.GetString() is { } embeddedText)
                    {
                        Log.Log(LogLevel.Information, AcpLogMessage.EmbeddedResource, new { Uri = embeddedUri, Chars = embeddedText.Length }, tag: Tag);
                        promptParts.Add(FormatAttachment(Path.GetFileName(embeddedUri), embeddedText));
                    }
                    break;

                case "resource_link" when IsAttachedByUser(item):
                    promptParts.Add(ReadAttachedFile(item));
                    break;
            }
        }

        return string.Join("\n\n", promptParts);
    }

    /// <summary>The active file's path, from the first resource_link in the prompt; null without one.</summary>
    public static string? ResourceLinkPath(JsonElement parameters)
    {
        if (parameters.TryGetProperty("prompt", out JsonElement promptItems) &&
            promptItems.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in promptItems.EnumerateArray())
            {
                if (item.TryGetProperty("type", out JsonElement typeElement))
                {
                    string itemType = typeElement.GetString() ?? "";
                    if (itemType == "resource_link" && item.TryGetProperty("uri", out JsonElement uriElement))
                    {
                        string uri = uriElement.GetString() ?? "";
                        if (uri.StartsWith("file:///"))
                        {
                            string path = uri.Substring(8); // Remove "file:///"
                            path = Uri.UnescapeDataString(path);
                            return path;
                        }
                    }
                }
            }
        }
        return null;
    }

    private static bool IsAttachedByUser(JsonElement resourceLink) =>
        !(resourceLink.TryGetProperty("description", out JsonElement description) &&
          (description.GetString() ?? "").Contains("opened in the IDE", StringComparison.OrdinalIgnoreCase));

    private string ReadAttachedFile(JsonElement resourceLink)
    {
        string uri = resourceLink.TryGetProperty("uri", out JsonElement uriElement) ? uriElement.GetString() ?? "" : "";
        string name = resourceLink.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() ?? "" : "";
        if (string.IsNullOrEmpty(name)) name = Path.GetFileName(uri);

        string? path = uri.StartsWith("file:///") ? paths.ResolveHostPath(Uri.UnescapeDataString(uri[8..])) : null;
        if (path is null)
        {
            Log.Log(LogLevel.Warning, AcpLogMessage.AttachedFileMissing, new { Uri = uri }, tag: Tag);
            return MessageText.Format(AcpModelText.AttachmentNotMounted, new { Name = name, Uri = uri });
        }

        try
        {
            string content = File.ReadAllText(path);
            Log.Log(LogLevel.Information, AcpLogMessage.AttachedFileRead, new { Name = name, Path = path, Chars = content.Length }, tag: Tag);
            return content.Length <= MaxAttachmentChars
                ? FormatAttachment(name, content, path)
                : FormatAttachment(name, content[..MaxAttachmentChars], path) +
                  MessageText.Format(AcpModelText.AttachmentTruncated, new
                  {
                      Shown = MaxAttachmentChars.ToString("N0", CultureInfo.InvariantCulture),
                      Length = content.Length.ToString("N0", CultureInfo.InvariantCulture),
                  });
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Warning, AcpLogMessage.AttachedFileReadFailed, new { Path = path, Reason = ex.Message }, tag: Tag);
            return MessageText.Format(AcpModelText.AttachmentUnreadable, new { Name = name, Reason = ex.Message });
        }
    }

    private static string FormatAttachment(string name, string content, string? containerPath = null) =>
        MessageText.Format(containerPath is null ? AcpModelText.Attachment : AcpModelText.AttachmentAt,
            new { Name = name, Path = containerPath, Content = content });
}
