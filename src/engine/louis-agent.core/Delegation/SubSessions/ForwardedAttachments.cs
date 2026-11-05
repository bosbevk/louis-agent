using LouisAgent.Composition.Messages;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions;

namespace LouisAgent.Core.Delegation.SubSessions;

/// <summary>
/// The model names the attachments; their contents come from the session, never through the model's output. A name
/// the session doesn't have gets the refusal that lists what is attached.
/// </summary>
internal static class ForwardedAttachments
{
    /// <summary>The named files with their contents to send on, or the refusal when one of them isn't attached.</summary>
    public static string? Take(string[]? attachments, out List<object> forwarded)
    {
        var available = SessionAttachments.Current;
        forwarded = [];
        foreach (string name in attachments ?? [])
        {
            if (!available.TryGetValue(name.Trim(), out string? content))
                return MessageText.Format(SubSessionToolMessage.AttachmentUnknown, new
                {
                    Name = name,
                    Attached = available.Count == 0 ? "(none)" : string.Join(", ", available.Keys.Order()),
                });
            forwarded.Add(new
            {
                name = name.Trim(),
                content
            });
        }

        return null;
    }
}
