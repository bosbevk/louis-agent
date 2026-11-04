using LouisAgent.Choices;

namespace LouisAgent.Orchestrator.Triage;

/// <summary>
/// What the triage agent decided about one error, read from the line it ends its answer with:
/// <c>DECISION: fixed - commit abc1234 verified</c>. If it wrote no such line, or named an outcome the prompt doesn't
/// offer, the outcome is unknown and the reason is the start of what it did say, so the comms log still shows
/// something.
/// </summary>
/// <remarks>
/// A small hand-written reader instead of a regular expression (ADR 0057): find the marker, take the word after it,
/// then one separator and the reason. Models dress the line up (<c>**DECISION:**</c>, a long dash, a colon), and each
/// of those is one plain trim or one character in <see cref="Separators"/>, which reads easier than the pattern it
/// replaces. Guard clauses keep each way a line can fail to one <c>return null</c> (ADR 0051).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
internal sealed record Decision(TriageOutcome Outcome, string Reason)
{
    /// <summary>What the triage prompt asks the agent to start its closing line with.</summary>
    private const string Marker = "DECISION:";

    /// <summary>What a model puts between the outcome and the reason: a hyphen, an en or em dash, a colon.</summary>
    private const string Separators = "-–—:";

    /// <summary>How much of an answer without a decision line we keep as its reason.</summary>
    private const int MaxReasonChars = 200;

    /// <summary>Reads the agent's answer. If it changed its mind along the way, its last decision counts.</summary>
    public static Decision Parse(string answer) =>
        answer.ReplaceLineEndings("\n").Split('\n').Select(FromLine).LastOrDefault(decision => decision is not null)
        ?? Unread(answer);

    /// <summary>One line's decision, or nothing when the line has no marker, no known outcome or no reason.</summary>
    private static Decision? FromLine(string line)
    {
        int at = line.IndexOf(Marker, StringComparison.Ordinal);
        if (at < 0) return null;

        // The outcome is the word right after the marker, parsed once into its enum (ADR 0024).
        string rest = Unbold(line[(at + Marker.Length)..]);
        int length = rest.TakeWhile(c => char.IsAsciiLetter(c) || c == '-').Count();
        if (length == 0) return null;
        if (!SettingChoice.TryParse(rest[..length], out TriageOutcome? outcome)) return null;
        if (outcome is not { } known) return null;

        // Then one separator, and everything after it is the reason.
        string tail = Unbold(rest[length..]);
        if (tail.Length == 0 || !Separators.Contains(tail[0])) return null;
        string reason = tail[1..].Trim();
        if (reason.Length == 0) return null;

        return new Decision(known, reason);
    }

    /// <summary>Drops the spaces and Markdown bold stars a model puts around the outcome.</summary>
    private static string Unbold(string text) => text.TrimStart().TrimStart('*').TrimStart();

    /// <summary>An answer without a decision line: unknown, with the answer itself, cut short, as its reason.</summary>
    private static Decision Unread(string answer)
    {
        string text = answer.ReplaceLineEndings(" ").Trim();
        string reason = text.Length > MaxReasonChars ? text[..MaxReasonChars] + "..." : text;
        return new Decision(TriageOutcome.Unknown, reason);
    }
}
