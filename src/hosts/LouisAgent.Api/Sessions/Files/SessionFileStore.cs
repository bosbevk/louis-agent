using System.Globalization;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Messages.Holes.Files;
using LouisAgent.Choices;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Files;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Api.Sessions.Files;

/// <summary>
/// The photos and PDFs a chat was sent, kept by name for a tool to read (F171.1). It checks a message's files before
/// it keeps any: each must be a kind a model reads and at most 10 MB, and the chat keeps at most 20 MB of files in
/// all. A file is kept in memory with the chat, as its history is, and never written to a log or to disk.
/// </summary>
/// <remarks>
/// The limits and the checks live here, on the type that keeps the files (ADR 0123), so the session's message handler
/// asks one question and stays within its size. The same name again replaces the file it had, as a text attachment
/// does. A lock holds the check and the keep together, so two messages at once can't both slip under the limit.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
internal sealed class SessionFileStore : ISessionFiles
{
    /// <summary>A megabyte, as the limits count one.</summary>
    private const int Megabyte = 1024 * 1024;

    /// <summary>The most one file may weigh: Claude's own limit for an image.</summary>
    public const int MaxFileMegabytes = 10;

    /// <summary>The most a chat keeps in files, all of them together: the price import's limit.</summary>
    public const int MaxSessionMegabytes = 20;

    private readonly Dictionary<string, SessionFile> _files = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks the files among a message's attachments and keeps them all, or keeps none and says why. A text
    /// attachment is not a file, and is left alone.
    /// </summary>
    public bool TryKeep(IReadOnlyList<AttachmentRequest> attachments, out string refusal)
    {
        var incoming = new List<SessionFile>();
        foreach (var attachment in attachments.Where(attachment => attachment.Bytes is not null))
        {
            if (!TryRead(attachment, out var file, out refusal)) return false;
            incoming.Add(file);
        }

        lock (_files)
        {
            // What the chat would hold: what it keeps that this message doesn't replace, then each new file in turn.
            long total = _files.Values.Where(kept => !incoming.Any(file => SameName(file, kept)))
                .Sum(kept => (long)kept.Bytes.Length);
            foreach (var file in incoming)
            {
                total += file.Bytes.Length;
                if (total <= MaxSessionMegabytes * Megabyte) continue;
                var full = new SessionFilesFullHoles(file.Name, Megabytes(total), MaxSessionMegabytes);
                refusal = MessageText.Format(ApiUserText.SessionFilesFull, full);
                return false;
            }

            foreach (var file in incoming) _files[file.Name] = file;
        }

        refusal = string.Empty;
        return true;
    }

    /// <inheritdoc/>
    public SessionFile? Find(string name)
    {
        lock (_files) return _files.GetValueOrDefault(name);
    }

    /// <inheritdoc/>
    public void Forget(string name)
    {
        lock (_files) _files.Remove(name);
    }

    /// <summary>Reads one attachment as a file, or says why it isn't one a model can read.</summary>
    private static bool TryRead(AttachmentRequest attachment, out SessionFile file, out string refusal)
    {
        file = null!;
        refusal = string.Empty;
        byte[] bytes = attachment.Bytes!;

        // A kind the one parser doesn't know, or none at all, is not a file a model reads.
        if (!SettingChoice.TryParse(attachment.MediaType, out FileMediaType? kind) || kind is not { } known)
        {
            var unknown = new UnknownFileKindHoles(attachment.Name, attachment.MediaType ?? "none",
                string.Join(", ", SettingChoice.Names<FileMediaType>()));
            refusal = MessageText.Format(ApiUserText.UnknownFileKind, unknown);
            return false;
        }

        if (bytes.Length > MaxFileMegabytes * Megabyte)
        {
            var tooLarge = new SessionFileTooLargeHoles(attachment.Name, Megabytes(bytes.Length), MaxFileMegabytes);
            refusal = MessageText.Format(ApiUserText.SessionFileTooLarge, tooLarge);
            return false;
        }

        file = new SessionFile(attachment.Name, known, bytes);
        return true;
    }

    /// <summary>Whether two files go by the same name, the case ignored, as the store keeps them.</summary>
    private static bool SameName(SessionFile one, SessionFile other) =>
        string.Equals(one.Name, other.Name, StringComparison.OrdinalIgnoreCase);

    /// <summary>A size in megabytes to one place, as a refusal says it: <c>10.0</c>.</summary>
    private static string Megabytes(long bytes) =>
        ((double)bytes / Megabyte).ToString("0.0", CultureInfo.InvariantCulture);
}
