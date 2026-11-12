using System.Globalization;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Messages.Holes.Attachments;
using LouisAgent.Composition.Messages;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Api.Sessions.Prompts;

/// <summary>
/// The prompt for a message: its text, then each attachment's contents (the API can't see the caller's disk), each
/// capped so one can't fill the context, then the page the visitor is reading (<see cref="PageBlock"/>, F115).
/// </summary>
public static class TurnPrompt
{
    /// <summary>The most of one attachment that goes to the model.</summary>
    private const int MaxAttachmentChars = 100_000;

    /// <summary>The prompt for <paramref name="request"/>: its parts, a blank line between them.</summary>
    public static string Of(SendMessageRequest request)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Message)) parts.Add(request.Message);
        parts.AddRange((request.Attachments ?? []).Select(AttachmentOf));
        if (request.Page is { } page && PageBlock.Of(page) is { Length: > 0 } block) parts.Add(block);
        return string.Join("\n\n", parts);
    }

    /// <summary>One attached file as a fenced block, cut with a note when it is long.</summary>
    private static string AttachmentOf(AttachmentRequest attachment)
    {
        string content = attachment.Content ?? string.Empty;
        string body = content.Length <= MaxAttachmentChars ? content : content[..MaxAttachmentChars];
        var file = new AttachmentHoles(attachment.Name, body);
        string block = MessageText.Format(ApiModelText.Attachment, file);
        if (content.Length <= MaxAttachmentChars) return block;
        var cut = new AttachmentTruncatedHoles(Count(MaxAttachmentChars), Count(content.Length));
        return block + MessageText.Format(ApiModelText.AttachmentTruncated, cut);
    }

    /// <summary>A count of characters as the model reads it: 100,000.</summary>
    private static string Count(int chars) => chars.ToString("N0", CultureInfo.InvariantCulture);
}
