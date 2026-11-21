namespace LouisAgent.Web.Chats.Calls.Forms;

/// <summary>
/// What a filled form sends: each field by its label, in the form's order, as one message, such as
/// <c>Name: John · Phone: 066 391 2470</c>. The agent reads it as the visitor's answer, and the card reads it back to
/// fold to what was sent.
/// </summary>
/// <remarks>
/// Reading back walks the form's own labels in order, so a value may hold anything, a dot or a new line, and a message
/// the visitor wrote themselves doesn't read as the form.
/// </remarks>
public static class FormAnswer
{
    /// <summary>Between two fields.</summary>
    private const string Between = " · ";

    /// <summary>Between a label and what was filled in.</summary>
    private const string After = ": ";

    /// <summary>The filled form as one message, each value trimmed.</summary>
    public static string Of(Asked form, IReadOnlyList<string> values) =>
        string.Join(Between, form.Fields.Select((field, at) => field.Label + After + values[at].Trim()));

    /// <summary>The values the message sent, in the form's order, or none when it wasn't the form.</summary>
    public static IReadOnlyList<string>? Read(Asked form, string message)
    {
        var values = new List<string>();
        int from = 0;
        for (int at = 0; at < form.Fields.Count; at++)
        {
            string opening = Opening(form, at);
            if (string.CompareOrdinal(message, from, opening, 0, opening.Length) != 0) return null;
            from += opening.Length;

            // The value runs to the next field's opening, or to the end after the last field.
            int end = at + 1 < form.Fields.Count
                ? message.IndexOf(Opening(form, at + 1), from, StringComparison.Ordinal)
                : message.Length;
            if (end < 0) return null;
            values.Add(message[from..end]);
            from = end;
        }

        return values;
    }

    /// <summary>What comes before a field's value: its label, after the field before it.</summary>
    private static string Opening(Asked form, int at) => (at == 0 ? "" : Between) + form.Fields[at].Label + After;
}
