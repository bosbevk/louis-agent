using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace LouisAgent.Evals.Runs.Store;

/// <summary>
/// A run's id: the UTC time it started and its eval's name, as <c>20261123-043012-guide</c>. So a folder listing is
/// already in order, and an id holds nothing that could lead a path out of its folder.
/// </summary>
/// <remarks>
/// The one place that knows what an id looks like (ADR 0123). It is read character by character, with no regular
/// expression (ADR 0057), and an id that doesn't read is never part of a path (ADR 0004).
/// </remarks>
/// <param name="StartedAt">When the run started, in UTC, to the second.</param>
/// <param name="Eval">The eval's name: letters, digits and hyphens.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: regex is the last option</seealso>
public sealed record RunId(DateTimeOffset StartedAt, string Eval)
{
    private const string TimeFormat = "yyyyMMdd-HHmmss";
    private const char Joint = '-';

    /// <summary>The id as it is written, in a file's name and on the run.</summary>
    public string Text =>
        $"{StartedAt.UtcDateTime.ToString(TimeFormat, CultureInfo.InvariantCulture)}{Joint}{Eval}";

    /// <summary>The id this text is, when it is one: a start time to the second, a hyphen, an eval's name.</summary>
    /// <remarks>Guard clauses (Fowler): one thing checked per line, the cheapest first.</remarks>
    public static bool TryParse(string? text, [NotNullWhen(true)] out RunId? id)
    {
        id = null;
        if (text is null || text.Length <= TimeFormat.Length + 1) return false;
        if (text[TimeFormat.Length] != Joint) return false;

        string eval = text[(TimeFormat.Length + 1)..];
        if (!IsEvalName(eval)) return false;
        if (!DateTime.TryParseExact(text[..TimeFormat.Length], TimeFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var started)) return false;

        id = new RunId(new DateTimeOffset(started, TimeSpan.Zero), eval);
        return true;
    }

    /// <summary>An eval's name holds only letters, digits and hyphens, so it is safe in a path.</summary>
    public static bool IsEvalName(string name) =>
        name.Length > 0 && name.All(letter => char.IsAsciiLetterOrDigit(letter) || letter == Joint);
}
