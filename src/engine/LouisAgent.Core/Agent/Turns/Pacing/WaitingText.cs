using System.Text;
using LouisAgent.SessionStream.Pacing;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.Turns.Pacing;

/// <summary>
/// The text a turn has sent that the paced stream has not yet handed out (F155): it says how much may go now, at
/// <see cref="WordPace"/>'s pace, and hands it out cut on a whole word, as an update like the turn's own.
/// </summary>
/// <remarks>
/// Ask the type (ADR 0123): the counting and the cutting are this class's, so the stream only reads, ticks and hands
/// out. It keeps the pace's part letters (<c>_shown</c>) apart from what it has sent, since a piece cut back to a whole
/// word sends less than the pace allowed, and the rest is owed on the next tick.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
internal sealed class WaitingText
{
    private readonly StringBuilder _waiting = new();

    /// <summary>The letters the turn has sent, all told.</summary>
    private int _arrived;

    /// <summary>The letters handed out, all told.</summary>
    private int _sent;

    /// <summary>The letters the pace allows by now, part letters kept; never fewer than were sent.</summary>
    private double _shown;

    /// <summary>The last text update the turn sent, whose role and ids a handed-out piece carries.</summary>
    private ChatResponseUpdate? _like;

    /// <summary>Whether nothing is waiting.</summary>
    public bool IsEmpty => _waiting.Length == 0;

    /// <summary>Whether this update is only text, so it waits to be paced; anything else goes out as it is.</summary>
    public static bool IsText(ChatResponseUpdate update) =>
        update is { Contents.Count: > 0, FinishReason: null } && update.Contents.All(content => content is TextContent);

    /// <summary>Text the turn sent, to wait its turn.</summary>
    public void Add(ChatResponseUpdate update)
    {
        string text = update.Text;
        _waiting.Append(text);
        _arrived += text.Length;
        _like = update;
    }

    /// <summary>
    /// The piece the pace allows after this long, cut back to its last whole word; none when not a whole word fits
    /// yet. Once the turn is done the pace finishes, and what is left goes out whole when the pace reaches it.
    /// </summary>
    public ChatResponseUpdate? Take(TimeSpan since, bool done)
    {
        _shown = WordPace.Advance(_shown, _arrived, since, done);
        int allowed = WordPace.CountLetters(_shown) - _sent;
        if (allowed <= 0) return null;

        // All that is waiting, when the pace reaches it; otherwise up to the last space or line within what is allowed.
        int cut = allowed >= _waiting.Length ? _waiting.Length : FindLastWordEnd(allowed);
        return cut == 0 ? null : Hand(cut);
    }

    /// <summary>Where the last whole word within the first letters allowed ends, its space or line included.</summary>
    private int FindLastWordEnd(int allowed)
    {
        for (int at = allowed - 1; at >= 0; at--)
        {
            if (char.IsWhiteSpace(_waiting[at])) return at + 1;
        }

        return 0;
    }

    /// <summary>Hands out the first letters waiting, as a text update like the turn's.</summary>
    private ChatResponseUpdate Hand(int letters)
    {
        string piece = _waiting.ToString(0, letters);
        _waiting.Remove(0, letters);
        _sent += letters;
        return new ChatResponseUpdate(_like!.Role, piece)
        {
            MessageId = _like.MessageId,
            ResponseId = _like.ResponseId,
            ModelId = _like.ModelId,
            CreatedAt = _like.CreatedAt,
            AuthorName = _like.AuthorName
        };
    }
}
