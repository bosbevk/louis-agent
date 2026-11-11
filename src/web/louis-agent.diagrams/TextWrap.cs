namespace LouisAgent.Diagrams;

/// <summary>
/// Wraps words into lines of at most so many characters of the code font, which every diagram draws its words in, so
/// a line's width is known without measuring: a box's note and a card's text wrap the same way.
/// </summary>
public static class TextWrap
{
    /// <summary>
    /// The text as lines: words taken in order while a line holds <paramref name="width"/> characters, a word too
    /// long for any line on one of its own.
    /// </summary>
    public static IReadOnlyList<string> Lines(string text, int width)
    {
        var lines = new List<string>();
        var line = "";
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                lines.Add(line);
                line = word;
                continue;
            }

            line = line.Length == 0 ? word : $"{line} {word}";
        }

        if (line.Length > 0) lines.Add(line);
        return lines;
    }
}
